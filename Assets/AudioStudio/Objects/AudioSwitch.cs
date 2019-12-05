using System;
using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{		
	[CreateAssetMenu(fileName = "New Switch", menuName = "Audio/Switch")]
	public class AudioSwitch : AudioController
	{	
		public List<string> SwitchNames = new List<string>();
		private List<AudioSwitchInstance> _audioSwitchInstances;
		public string DefaultSwitch;

		#region Initialize
		public override void Init()
		{
			_audioSwitchInstances = new List<AudioSwitchInstance>();
		}

		public override void Dispose()
		{
			_audioSwitchInstances = null;
		}
		#endregion

		public void SetSwitchGlobal(string newSwitch, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!SwitchNames.Contains(newSwitch))
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, name, null, "Can't find switch named " + newSwitch);
				return;
			}
			foreach (var asi in _audioSwitchInstances)
			{
				asi.SetSwitch(newSwitch, trigger);
			}					
		}
		
		public void SetSwitch(string newSwitch, GameObject affectedGameObject, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!SwitchNames.Contains(newSwitch))
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, name, null, "Can't find switch named " + newSwitch);
				return;
			}			
			GetOrAddSwitchInstance(affectedGameObject).SetSwitch(newSwitch, trigger);			
		}

		public string GetSwitch(GameObject affectedGameObject)
		{			
			var asi = GetOrAddSwitchInstance(affectedGameObject);
			var switchName = asi.CurrentSwitch;
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Switch, AudioAction.GetValue, AudioTriggerSource.Code, name, asi.gameObject, "Switch is at " + switchName);							
			return switchName;
		}

		public AudioSwitchInstance GetOrAddSwitchInstance(GameObject gameObject)
		{
			var instances = gameObject.GetComponents<AudioSwitchInstance>();
			foreach (var asi in instances)
			{
				if (asi.AudioSwitch == this)
					return asi;
			}
			var newInstance = gameObject.AddComponent<AudioSwitchInstance>();
			newInstance.Init(this);
			return newInstance;
		}
		
		#region Editor
		public override void CleanUp()
		{
			foreach (var switchName in SwitchNames)
			{
				if (string.IsNullOrEmpty(switchName))
					Debug.LogError("SwitchName of AudioSwitch " + name + " is empty!");
			}
		}
		#endregion
		
		public class AudioSwitchInstance : MonoBehaviour
		{
			public AudioSwitch AudioSwitch;
			public string CurrentSwitch;		
			public Action<GameObject> OnSwitchChanged;

			public void Init(AudioSwitch swc)
			{
				AudioSwitch = swc;
				CurrentSwitch = swc.DefaultSwitch;			
			}
		
			public void SetSwitch(string newSwitch, AudioTriggerSource trigger = AudioTriggerSource.Code)
			{
				if (CurrentSwitch != newSwitch)
				{
					CurrentSwitch = newSwitch;
					OnSwitchChanged?.Invoke(gameObject);
					AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Switch, AudioAction.SetValue, trigger, name, gameObject, "Switch is set at " + newSwitch);
				}
				else
					AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Switch, AudioAction.SetValue, trigger, name, gameObject, "The same switch is already set");					
			}
		}		
	}		
		
    [Serializable]
    public struct SwitchEventMapping
    {		
        public string SwitchName;
        public AudioEvent AudioEvent;	    
    }
	
	[Serializable]
	public struct SwitchClipMapping
	{		
		public string SwitchName;
		public AudioClip Clip;	    
	}
}
