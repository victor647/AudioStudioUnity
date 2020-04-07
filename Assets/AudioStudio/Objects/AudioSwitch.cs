using System;
using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{		
	[CreateAssetMenu(fileName = "New Switch", menuName = "AudioStudio/Controller/Switch")]
	public class AudioSwitch : AudioController
	{	
		#region Fields
		public List<string> SwitchNames = new List<string>();
		public string DefaultSwitch;
		public float CooldownTime;
		#endregion
		
		#region Initialize
		private List<AudioSwitchInstance> _activeInstances;

		internal override void Init()
		{
			_activeInstances = new List<AudioSwitchInstance>();
		}

		internal override void Dispose()
		{
			foreach (var asi in _activeInstances)
			{
				Destroy(asi);
			}
		}
		
		internal void AddInstance(AudioSwitchInstance instance)
		{
			_activeInstances.Add(instance);
			AudioManager.GlobalSwitchInstances.Add(name +  " @ " + instance.gameObject.name);  
		}

		internal void RemoveInstance(AudioSwitchInstance instance)
		{
			_activeInstances.Remove(instance);
			AudioManager.GlobalSwitchInstances.Remove(name +  " @ " + instance.gameObject.name);  
		}
		#endregion

		public void SetSwitchGlobal(string newSwitch, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!SwitchNames.Contains(newSwitch))
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, name + " / " + newSwitch, null, "Invalid Switch name");
				return;
			}
			foreach (var asi in _activeInstances)
			{
				asi.SetSwitch(newSwitch, trigger);
			}					
		}
		
		public void SetSwitch(string newSwitch, GameObject affectedGameObject, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			if (!SwitchNames.Contains(newSwitch))
			{
				AsUnityHelper.DebugToProfiler(Severity.Error, AudioObjectType.Switch, AudioAction.SetValue, trigger, name + " / " + newSwitch, affectedGameObject, "Invalid Switch name");
				return;
			}			
			GetOrAddSwitchInstance(affectedGameObject).SetSwitch(newSwitch, trigger);			
		}

		public string GetSwitch(GameObject affectedGameObject)
		{			
			var asi = GetOrAddSwitchInstance(affectedGameObject);
			var switchName = asi.CurrentSwitch;
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Switch, AudioAction.GetValue, AudioTriggerSource.Code, name + " / " + switchName, asi.gameObject);							
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
	}		
	
	public class AudioSwitchInstance : AudioControllerInstance
	{
		public AudioSwitch AudioSwitch;
		public string CurrentSwitch;		
		public Action<GameObject> OnSwitchChanged;
		private float _lastSetTimeStamp = -10000;
		private string _pendingSwitch;

		public void Init(AudioSwitch swc)
		{
			AudioSwitch = swc;
			CurrentSwitch = _pendingSwitch = swc.DefaultSwitch;		
			AudioSwitch.AddInstance(this);
		}

		private void OnDestroy()
		{
			AudioSwitch.RemoveInstance(this);
		}

		public void SetSwitch(string newSwitch, AudioTriggerSource trigger = AudioTriggerSource.Code)
		{
			_pendingSwitch = newSwitch;
			if (CurrentSwitch != newSwitch)
			{
				if (AudioSwitch.CooldownTime == 0f)
					DoSetSwitch(newSwitch, trigger);
				else if (Time.time - _lastSetTimeStamp >= AudioSwitch.CooldownTime)
				{
					_lastSetTimeStamp = Time.time;
					DoSetSwitch(newSwitch, trigger);
				}
				else
					AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Switch, AudioAction.SetValue, trigger, AudioSwitch.name + " / " + newSwitch, gameObject, "Switch still in cooldown");
			}
			else
				AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Switch, AudioAction.SetValue, trigger, AudioSwitch.name + " / " + newSwitch, gameObject, "Same switch already set");
		}

		private void DoSetSwitch(string newSwitch, AudioTriggerSource trigger)
		{
			CurrentSwitch = newSwitch;
			OnSwitchChanged?.Invoke(gameObject);
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Switch, AudioAction.SetValue, trigger, AudioSwitch.name + " / " + newSwitch, gameObject);
		}

		private void Update()
		{
			if (AudioSwitch.CooldownTime > 0f && _pendingSwitch != CurrentSwitch && Time.time - _lastSetTimeStamp >= AudioSwitch.CooldownTime)
			{
				SetSwitch(_pendingSwitch);
				_pendingSwitch = string.Empty;
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
