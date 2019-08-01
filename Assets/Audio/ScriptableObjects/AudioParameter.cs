using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioStudio.Configs
{
	[CreateAssetMenu(fileName = "New Parameter", menuName = "Audio/Parameter")]
	public class AudioParameter : AudioController
	{								
		public float MinValue;		
		public float MaxValue = 100f;								
		public bool Slew;		
		public float SlewRate = 20f;
		public float DefaultValue = 50f;
		
		private List<AudioParameterInstance> _audioParameterInstances;

		#region Initialize
		public override void Init()
		{			
			_audioParameterInstances = new List<AudioParameterInstance>();
		}
		
		public override void Dispose()
		{             
			_audioParameterInstances = null;											            
		}
		#endregion
		
		public void RegisterMapping(ParameterMapping mapping, GameObject gameObject)
		{
			GetOrAddParameterInstance(gameObject);
			foreach (var api in _audioParameterInstances)
			{
				if (api.gameObject == gameObject)
					api.ParameterMappings.Add(mapping);
			}															
		}

		public void UnRegisterMapping(ParameterMapping mapping, GameObject gameObject)
		{
			foreach (var api in _audioParameterInstances)
			{
				if (api.gameObject == gameObject)
					api.ParameterMappings.Remove(mapping);
			}
		}

		public void SetValueGlobal(float newValue)
		{
			foreach (var api in _audioParameterInstances)
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
			AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Parameter, AudioAction.GetValue, name, api.gameObject.name, "Parameter is at value " + value);
			return value;
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
		
		#region Editor
		public override void CleanUp()
		{
			if (MinValue >= MaxValue)
				Debug.LogError("Min Value in AudioParameter " + name + " >= Max Value!");		
			if (DefaultValue < MinValue || DefaultValue > MaxValue)
				Debug.LogError("Default Value in AudioParameter " + name + " out of range!");
		}
		#endregion
		
		public class AudioParameterInstance : MonoBehaviour
		{			
			public AudioParameter Parameter;
			public List<ParameterMapping> ParameterMappings = new List<ParameterMapping>();
			public float CurrentValue;
			private bool _isSlewing;
			private float _targetSetValue;

			private void OnDestroy()
			{
				Parameter._audioParameterInstances.Remove(this);
			}

			public void Init(AudioParameter parameter)
			{
				Parameter = parameter;								
				CurrentValue = _targetSetValue = Parameter.DefaultValue;					
				parameter._audioParameterInstances.Add(this);
			}
		
			public void SetParameterValue(float newValue)
			{
				if (_targetSetValue == newValue) return;
				AudioManager.DebugToProfiler(ProfilerMessageType.Notification, ObjectType.Parameter, AudioAction.SetValue, name, gameObject.name, "Set Parameter value at " + newValue.ToString("0.000"));
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
			private void FixedUpdate()
			{				
				if (_isSlewing)				
					SlewValue(CurrentValue); 				
			}

			//apply the value change to all the mappings
			private void ApplyToMappings()
			{
				foreach (var mapping in ParameterMappings)
				{
					mapping.ConvertParameterToTarget(CurrentValue, gameObject); 
				}	
			}
		}
	}		
	
	public enum TargetType
	{
		Volume,
		Pan, 
		Pitch,
		LowPassFilterCutoff,
		HighPassFilterCutoff
	} 
	
	[Serializable]
	public class ParameterMapping
    {        
        public AudioParameterReference AudioParameterReference;        
        public TargetType TargetType = TargetType.Volume;	    
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
        
	    public void Init(AudioEventInstance evt, GameObject go)
	    {		   
		    _affectedEventInstance = evt;
		    _parameter = AudioAssetLoader.GetAudioParameter(AudioParameterReference.Name);
		    if (!_parameter) return;
		    _parameter.RegisterMapping(this, go);	
		    
		    switch (TargetType)
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
		    ConvertParameterToTarget(_parameter.GetValue(go), go);
	    }

	    public void Dispose(GameObject go)
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
	    
        public void ConvertParameterToTarget(float value, GameObject go)
        {
            float parameterPercentage;
            if (MinParameterValue > MaxParameterValue)
            {
                parameterPercentage = 1f - (Mathf.Clamp(value, MaxParameterValue, MinParameterValue) - MaxParameterValue) 
                                      / (MinParameterValue - MaxParameterValue);
            }
            else
            {
                parameterPercentage = (Mathf.Clamp(value, MinParameterValue, MaxParameterValue) - MinParameterValue) 
                                      / (MaxParameterValue - MinParameterValue);
            }
            
            var targetValue = Mathf.Lerp(MinTargetValue, MaxTargetValue, Mathf.Pow(parameterPercentage, CurveExponent));            
            _modifyTarget(targetValue, go);                     
        }
    }
}