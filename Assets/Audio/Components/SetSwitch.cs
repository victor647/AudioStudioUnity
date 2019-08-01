using System.Linq;
using AudioStudio.Configs;
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
			AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SetSwitch, AudioAction.Activate, "OnEnable", gameObject.name);
			SetSwitchValue(OnSwitches);
		}

		protected override void HandleDisableEvent()
		{	
			if (SetOn != TriggerCondition.EnableDisable) return;
			AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SetSwitch, AudioAction.Deactivate, "OnDisable", gameObject.name);
			SetSwitchValue(OffSwitches);
		}
		
		private void OnTriggerEnter(Collider other)
        {
            if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;            
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SetSwitch, AudioAction.Activate, "OnTriggerEnter", gameObject.name, "Enter with " + other.gameObject.name);
            SetSwitchValue(OnSwitches);                        
        }

        private void OnTriggerExit(Collider other)
        {
            if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;            
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SetSwitch, AudioAction.Deactivate, "OnTriggerExit", gameObject.name, "Exit with " + other.gameObject.name);
            SetSwitchValue(OffSwitches);                
        }      
        
        private void OnCollisionEnter(Collision other)
        {
            if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;            
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SetSwitch, AudioAction.Activate, "OnCollisionEnter", gameObject.name, "Enter with " + other.gameObject.name);
            SetSwitchValue(OnSwitches);                        
        }

        private void OnCollisionExit(Collision other)
        {
            if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;            
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.SetSwitch, AudioAction.Deactivate, "OnCollisionExit", gameObject.name, "Exit with " + other.gameObject.name);
            SetSwitchValue(OffSwitches);                          
        }

        private void SetSwitchValue(SetSwitchReference[] switches)
        {
	        if (IsGlobal)
	        {
		        foreach (var swc in switches)
		        {
			        swc.SetValueGlobal();    
		        }
	        }
	        else
	        {
		        foreach (var swc in switches)
		        {
			        swc.SetValue(gameObject);  
		        }
	        }		        
        }
        
        public override bool IsValid()
        {
	        return OnSwitches.Any(s => s.IsValid()) || OffSwitches.Any(s => s.IsValid());
        }
	}
}