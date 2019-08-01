using System;
using System.Collections.Generic;
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

		public void SetSwitchGlobal(string newSwitch)
		{
			if (!SwitchNames.Contains(newSwitch))
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Switch, AudioAction.SetValue, name, "Global", "Can't find switch named " + newSwitch);
				return;
			}
			foreach (var asi in _audioSwitchInstances)
			{
				asi.SetSwitch(newSwitch);
			}					
		}
		
		public void SetSwitch(string newSwitch, GameObject affectedGameObject)
		{
			if (!SwitchNames.Contains(newSwitch))
			{
				AudioManager.DebugToProfiler(ProfilerMessageType.Error, ObjectType.Switch, AudioAction.SetValue, name, "Global", "Can't find switch named " + newSwitch);
				return;
			}			
			GetOrAddSwitchInstance(affectedGameObject).SetSwitch(newSwitch);			
		}

		public string GetSwitch(GameObject affectedGameObject)
		{			
			var asi = GetOrAddSwitchInstance(affectedGameObject);
			var switchName = asi.CurrentSwitch;
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Switch, AudioAction.GetValue, name, asi.gameObject.name, "Switch is at " + switchName);							
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
		
			public void SetSwitch(string newSwitch)
			{
				if (CurrentSwitch != newSwitch)
				{
					CurrentSwitch = newSwitch;
					OnSwitchChanged?.Invoke(gameObject);
					AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Switch, AudioAction.SetValue, name, gameObject.name, "Switch is set at " + newSwitch);
				}
				else
					AudioManager.DebugToProfiler(ProfilerMessageType.Warning, ObjectType.Switch, AudioAction.SetValue, name, gameObject.name, "The same switch is already set");					
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
