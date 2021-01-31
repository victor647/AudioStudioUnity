using System;
using System.Collections.Generic;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Configs
{
	[CreateAssetMenu(fileName = "New Parameter", menuName = "AudioStudio/Controller/Parameter")]
	public class AudioParameter : AudioController
	{							
		#region Fields
		public float MinValue;		
		public float MaxValue = 100f;								
		public bool Slew;		
		public float SlewRate = 20f;
		public float DefaultValue = 50f;
		#endregion

		#region Initialize
		private readonly List<AudioParameterInstance> _activeInstances = new List<AudioParameterInstance>();

		internal override void Init()
		{			
			_activeInstances.Clear();
		}

		internal override void Dispose()
		{             
			foreach (var api in _activeInstances)
			{
				Destroy(api);
			}
		}
		
		internal void RegisterMapping(ParameterMapping mapping, GameObject gameObject)
		{
			var api = GetOrAddParameterInstance(gameObject);
			api.ParameterMappings.Add(mapping);														
		}

		internal void UnRegisterMapping(ParameterMapping mapping, GameObject gameObject)
		{
			foreach (var api in _activeInstances)
			{
				if (api.gameObject == gameObject)
					api.ParameterMappings.Remove(mapping);
			}
		}
		
		internal void AddInstance(AudioParameterInstance instance)
		{
			_activeInstances.Add(instance);
			EmitterManager.AddParameterInstance(instance);
		}

		internal void RemoveInstance(AudioParameterInstance instance)
		{
			_activeInstances.Remove(instance);
			EmitterManager.RemoveParameterInstance(instance);  
		}
		
		private AudioParameterInstance GetOrAddParameterInstance(GameObject gameObject)
		{
			var instances = gameObject.GetComponents<AudioParameterInstance>();
			foreach (var api in instances)
			{
				if (api.Parameter == this)
					return api;
			}
			var newInstance = gameObject.AddComponent<AudioParameterInstance>();
			newInstance.Init(this);
			return newInstance;
		}
		#endregion
		
		#region Controls
		public void SetValueGlobal(float newValue)
		{
			foreach (var api in _activeInstances)
			{
				api.SetParameterValue(newValue);
			}			
		}
		
		public void SetValue(float newValue, GameObject affectedGameObject)
		{									
			GetOrAddParameterInstance(affectedGameObject).SetParameterValue(newValue);							
		}
		
		public float GetValue(GameObject affectedGameObject)
		{			
			var api = GetOrAddParameterInstance(affectedGameObject);
			var value = api.CurrentValue;
			AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Parameter, AudioAction.GetValue, AudioTriggerSource.Code, name, api.gameObject, value.ToString("0.000"));
			return value;
		}
		#endregion
		
		#region Editor
		public override void CleanUp()
		{
			if (MinValue >= MaxValue)
				Debug.LogError("Min Value in AudioParameter " + name + " >= Max Value!");		
			if (DefaultValue < MinValue || DefaultValue > MaxValue)
				Debug.LogError("Default Value in AudioParameter " + name + " out of range!");
		}
		#endregion
	}		
	
	public class AudioParameterInstance : AudioControllerInstance
	{			
		public AudioParameter Parameter;
		public List<ParameterMapping> ParameterMappings = new List<ParameterMapping>();
		public float CurrentValue;
		private bool _isSlewing;
		private float _targetSetValue;

		private void OnDestroy()
		{
			Parameter.RemoveInstance(this);
		}

		internal void Init(AudioParameter parameter)
		{
			Parameter = parameter;								
			CurrentValue = _targetSetValue = Parameter.DefaultValue;					
			Parameter.AddInstance(this);
		}

		internal void SetParameterValue(float newValue)
		{
			if (_targetSetValue == newValue) return;
			AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Parameter, AudioAction.SetValue, AudioTriggerSource.Code, name, gameObject, newValue.ToString("0.000"));
			_targetSetValue = newValue;		
			SlewValue(newValue);
		}

		private void SlewValue(float value)
		{
			value = Mathf.Clamp(value, Parameter.MinValue, Parameter.MaxValue);			

			if (!Parameter.Slew) 				
				CurrentValue = value;				
			else //slewing the parameter change rate
			{
				if (Mathf.Abs(CurrentValue - _targetSetValue) > Parameter.SlewRate * 0.01f) //if still slewing
				{
					_isSlewing = true;
					//increment the slewing value
					CurrentValue += Mathf.Sign(_targetSetValue - CurrentValue) * Parameter.SlewRate * Time.fixedDeltaTime;                    
				}
				else //slewing has finished
				{
					_isSlewing = false;
					CurrentValue = _targetSetValue; 
				}
			}			
			ApplyToMappings();		                     
		}
		
		//continue to slew using the current parameter as input
		internal void UpdateSlewValues()
		{				
			if (_isSlewing)				
				SlewValue(CurrentValue); 				
		}

		//apply the value change to all the mappings
		private void ApplyToMappings()
		{
			foreach (var mapping in ParameterMappings)
			{
				mapping.ApplyParameterChange(CurrentValue, gameObject); 
			}	
		}
	}
	
	
	
	[Serializable]
	public class ParameterMapping
    {        
	    public enum TargetType
	    {
		    Volume,
		    Pan, 
		    Pitch,
		    LowPassFilterCutoff,
		    HighPassFilterCutoff
	    } 
	    
        public AudioParameterReference AudioParameterReference;        
        public TargetType Target = TargetType.Volume;	    
        public float CurveExponent;       
        public float MinTargetValue;       
        public float MaxTargetValue;     
        public float MinParameterValue;       
        public float MaxParameterValue;
        
        private AudioParameter _parameter;
        private AudioEventInstance _affectedEventInstance;
        private Action<float, GameObject> _modifyTarget;

        public ParameterMapping()
        {
	        AudioParameterReference = new AudioParameterReference(); 
	        CurveExponent = 1f; 
	        MaxTargetValue = 1f; 
	        MaxParameterValue = 100f;
        }

        internal void Init(AudioEventInstance evt, GameObject go)
	    {		   
		    _affectedEventInstance = evt;
		    _parameter = AsAssetLoader.GetAudioParameter(AudioParameterReference.Name);
		    if (!_parameter) return;
		    _parameter.RegisterMapping(this, go);	
		    
		    switch (Target)
		    {
			    case TargetType.Volume:
				    _modifyTarget = SetVolume;				    
				    break;
			    case TargetType.Pitch:
				    _modifyTarget = SetPitch;
				    break;
			    case TargetType.Pan:
				    _modifyTarget = SetPan;
				    break;
			    case TargetType.LowPassFilterCutoff:
				    _modifyTarget = SetLowPassCutoff;
				    break;
			    case TargetType.HighPassFilterCutoff:
				    _modifyTarget = SetHighPassCutoff;
				    break;
		    }
		    ApplyParameterChange(_parameter.GetValue(go), go);
	    }

	    internal void Dispose(GameObject go)
	    {		    
			_parameter.UnRegisterMapping(this, go);
			_affectedEventInstance = null;
			_modifyTarget = null;
	    }

	    private void SetVolume(float volume, GameObject go)
	    {
		    _affectedEventInstance.SetVolume(volume);		    
	    }

	    private void SetPitch(float pitch, GameObject go)
	    {
		    _affectedEventInstance.SetPitch(pitch);	    
	    }
	    
	    private void SetPan(float pan, GameObject go)
	    {
		    _affectedEventInstance.SetPan(pan);		    
	    }
	    
	    private void SetHighPassCutoff(float cutoff, GameObject go)
	    {
		    _affectedEventInstance.SetHighPassCutoff(cutoff);
	    }
	    
	    private void SetLowPassCutoff(float cutoff, GameObject go)
	    {
		    _affectedEventInstance.SetLowPassCutoff(cutoff);
	    }

	    internal void ApplyParameterChange(float value, GameObject go)
	    {
		    var targetValue = ConvertParameterToTarget(value, MinParameterValue, MaxParameterValue, MinTargetValue, MaxTargetValue, CurveExponent);      
            _modifyTarget?.Invoke(targetValue, go);                     
        }

	    internal static float ConvertParameterToTarget(float parameterValue, float minParameterValue, float maxParameterValue, float minTargetValue, float maxTargetValue, float curveExponent = 1f)
	    {
		    parameterValue = Mathf.Clamp(parameterValue, minParameterValue, maxParameterValue);
		    var parameterPercentage = (parameterValue - minParameterValue) / (maxParameterValue - minParameterValue);
		    var targetValue = Mathf.Lerp(minTargetValue, maxTargetValue, Mathf.Pow(parameterPercentage, curveExponent));
		    return targetValue;
	    }
    }
}