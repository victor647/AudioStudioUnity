using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
	public class SetSwitch : AudioPhysicsHandler
	{		
		public SetSwitchReference[] OnSwitches = new SetSwitchReference[0];
		public SetSwitchReference[] OffSwitches = new SetSwitchReference[0];
		public bool IsGlobal;
		
		protected override void HandleEnableEvent()
		{			
			if (SetOn != TriggerCondition.EnableDisable) return;
			SetSwitchValue(OnSwitches);
		}

		protected override void HandleDisableEvent()
		{	
			if (SetOn != TriggerCondition.EnableDisable) return;
			SetSwitchValue(OffSwitches);
		}
		
		private void OnTriggerEnter(Collider other)
        {
            if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;
            SetSwitchValue(OnSwitches);                        
        }

        private void OnTriggerExit(Collider other)
        {
            if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;
            SetSwitchValue(OffSwitches);                
        }      
        
        private void OnCollisionEnter(Collision other)
        {
            if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;
            SetSwitchValue(OnSwitches);                        
        }

        private void OnCollisionExit(Collision other)
        {
            if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;
            SetSwitchValue(OffSwitches);                          
        }

        private void SetSwitchValue(SetSwitchReference[] switches)
        {
	        if (IsGlobal)
	        {
		        foreach (var swc in switches)
		        {
			        swc.SetValueGlobal(AudioTriggerSource.SetSwitch);    
		        }
	        }
	        else
	        {
		        foreach (var swc in switches)
		        {
			        swc.SetValue(gameObject, AudioTriggerSource.SetSwitch);  
		        }
	        }		        
        }
        
        public override bool IsValid()
        {
	        return OnSwitches.Any(s => s.IsValid()) || OffSwitches.Any(s => s.IsValid());
        }
	}
}