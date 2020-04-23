using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
	public class SetSwitch : AsPhysicsHandler
	{		
		public SetSwitchReference[] OnSwitches = new SetSwitchReference[0];
		public SetSwitchReference[] OffSwitches = new SetSwitchReference[0];
		public bool IsGlobal;
		
		public override void Activate(GameObject source = null)
		{
			SetSwitches(OnSwitches, source);
		}

		public override void Deactivate(GameObject source = null)
		{
			SetSwitches(OffSwitches, source);
		}
		
		protected override void HandleEnableEvent()
		{            
			if (SetOn != TriggerCondition.EnableDisable || OnSwitches.Length == 0) return;
			Activate(gameObject);
		}
        
		protected override void HandleDisableEvent()
		{            
			if (SetOn != TriggerCondition.EnableDisable) return;
			Deactivate(gameObject);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (SetOn != TriggerCondition.TriggerEnterExit || OnSwitches.Length == 0 || !CompareAudioTag(other)) return;
			Activate(GetEmitter(other.gameObject));
		}

		private void OnTriggerExit(Collider other)
		{
			if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;
			Deactivate(GetEmitter(other.gameObject));                           
		}      
        
		private void OnCollisionEnter(Collision other)
		{
			if (SetOn != TriggerCondition.CollisionEnterExit || OnSwitches.Length == 0 || !CompareAudioTag(other.collider)) return;
			Activate(GetEmitter(other.gameObject));                           
		}

		private void OnCollisionExit(Collision other)
		{
			if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;
			Deactivate(GetEmitter(other.gameObject));                           
		} 

        private void SetSwitches(SetSwitchReference[] switches, GameObject go)
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
			        swc.SetValue(go, AudioTriggerSource.SetSwitch);  
		        }
	        }		        
        }
        
        public override bool IsValid()
        {
	        return OnSwitches.Any(s => s.IsValid()) || OffSwitches.Any(s => s.IsValid());
        }
	}
}