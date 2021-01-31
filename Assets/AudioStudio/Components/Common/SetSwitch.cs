using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
	[AddComponentMenu("AudioStudio/Set Switch")]
	[DisallowMultipleComponent]
	public class SetSwitch : AsTriggerHandler
	{		
		public SetSwitchReference[] OnSwitches = new SetSwitchReference[0];
		public SetSwitchReference[] OffSwitches = new SetSwitchReference[0];
		public bool IsGlobal;
		
		public override void Activate(int index = 0)
		{
			SetSwitches(OnSwitches, GetEmitter, index);
		}

		public override void Deactivate(int index = 0)
		{
			SetSwitches(OffSwitches, GetEmitter, index);
		}
		
		protected override void HandleEnableEvent()
		{            
			if (SetOn != TriggerCondition.EnableDisable || OnSwitches.Length == 0) return;
			Activate();
		}
        
		protected override void HandleDisableEvent()
		{            
			if (SetOn != TriggerCondition.EnableDisable) return;
			Deactivate();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (SetOn != TriggerCondition.TriggerEnterExit || OnSwitches.Length == 0 || !CompareAudioTag(other)) return;
			Activate();
		}

		private void OnTriggerExit(Collider other)
		{
			if (SetOn != TriggerCondition.TriggerEnterExit || !CompareAudioTag(other)) return;
			Deactivate();                           
		}      
        
		private void OnCollisionEnter(Collision other)
		{
			if (SetOn != TriggerCondition.CollisionEnterExit || OnSwitches.Length == 0 || !CompareAudioTag(other.collider)) return;
			Activate();                           
		}

		private void OnCollisionExit(Collision other)
		{
			if (SetOn != TriggerCondition.CollisionEnterExit || !CompareAudioTag(other.collider)) return;
			Deactivate();                           
		} 

        private void SetSwitches(SetSwitchReference[] switches, GameObject go, int index = 0)
        {
	        if (IsGlobal)
	        {
		        for (var i = 0; i < switches.Length; i++)
		        {
			        if (index == 0 || index == i + 1)
				        switches[i].SetValueGlobal(AudioTriggerSource.SetSwitch);
		        }
	        }
	        else
	        {
		        for (var i = 0; i < switches.Length; i++)
		        {
			        if (index == 0 || index == i + 1)
				        switches[i].SetValue(go, AudioTriggerSource.SetSwitch);
		        }
	        }		        
        }
        
        public override bool IsValid()
        {
	        return OnSwitches.Any(s => s.IsValid()) || OffSwitches.Any(s => s.IsValid());
        }
	}
}