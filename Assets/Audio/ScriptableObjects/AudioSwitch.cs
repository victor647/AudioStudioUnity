using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioStudio
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
				AudioManager.DebugToProfiler(MessageType.Error, ObjectType.Switch, AudioAction.SetValue, name, "Global", "Can't find switch named " + newSwitch);
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
				AudioManager.DebugToProfiler(MessageType.Error, ObjectType.Switch, AudioAction.SetValue, name, "Global", "Can't find switch named " + newSwitch);
				return;
			}			
			GetOrAddSwitchInstance(affectedGameObject).SetSwitch(newSwitch);			
		}

		public string GetSwitch(GameObject affectedGameObject)
		{			
			var asi = GetOrAddSwitchInstance(affectedGameObject);
			var switchName = asi.CurrentSwitch;
			AudioManager.DebugToProfiler(MessageType.Notification, ObjectType.Switch, AudioAction.GetValue, name, asi.gameObject.name, "Switch is at " + switchName);							
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
					AudioManager.DebugToProfiler(MessageType.Notification, ObjectType.Switch, AudioAction.SetValue, name, gameObject.name, "Switch is set at " + newSwitch);
				}
				else
					AudioManager.DebugToProfiler(MessageType.Warning, ObjectType.Switch, AudioAction.SetValue, name, gameObject.name, "The same switch is already set");					
			}
		}		
	}		
		
    [Serializable]
    public class SwitchEventMapping
    {		
        public string SwitchName;
        public AudioEvent AudioEvent;	    
    }
	
	[Serializable]
	public class SwitchClipMapping
	{		
		public string SwitchName;
		public AudioClip Clip;	    
	}
}
