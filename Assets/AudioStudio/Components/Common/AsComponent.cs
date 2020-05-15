using System.Collections.Generic;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
    public abstract class AsComponent : MonoBehaviour
    {
        //if component is empty, destroy it to optimize performance
        protected virtual void Awake()
        {
            if (AudioManager.DisableAudio) 
                Destroy(this);
            if (!IsValid())
                AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Component, AudioAction.Activate, AudioTriggerSource.Initialization, GetType().Name, gameObject, "Component is empty");
        }

        private bool _started;
        private bool _enabled;

        private void OnEnable()
        {
            if (_started && !_enabled)
            {
                HandleEnableEvent();
                _enabled = true;
            }                                                                       
        }

        //make sure the first time it is played at Start instead of OnEnable
        protected virtual void Start()
        {
            _started = true;
            if (!_enabled)
            {
                HandleEnableEvent();
                _enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_started && _enabled)
            {
                HandleDisableEvent();
                _enabled = false;
            }            
        }
        
        protected virtual void HandleEnableEvent(){}
        protected virtual void HandleDisableEvent(){}
        
        //check if the component is empty
        public virtual bool IsValid()
        {
            return true;
        }
        
        //shortcut for posting multiple events
        protected static void PostEvents(IEnumerable<PostEventReference> events, AudioTriggerSource trigger, GameObject soundSource = null)
        {
            foreach (var evt in events)
            {				
                evt.Post(soundSource, trigger);
            }  
        }
    }

    public abstract class AsUIHandler : AsComponent
    {
        protected override void HandleEnableEvent()
        {
            AddListener();
        }

        protected override void HandleDisableEvent()
        {
            RemoveListener();
        }

        public virtual void AddListener() {}
        public virtual void RemoveListener() {}
    }
    
    /// <summary>
    /// Sound emitter when dealing with collision.
    /// </summary>
    public enum PostFrom
    {
        Self,
        Other
    }
    
    /// <summary>
    /// Define when the event is triggered. 
    /// </summary>
    public enum TriggerCondition
    {
        AwakeDestroy,
        EnableDisable,
        TriggerEnterExit,
        CollisionEnterExit,   
        ManuallyControl
    }

    // for any components that can be triggered by physics
    public abstract class AsTriggerHandler : AsComponent
    {
        public TriggerCondition SetOn = TriggerCondition.EnableDisable;
        public PostFrom PostFrom = PostFrom.Self;
        [EnumFlag(typeof(AudioTags))]
        public AudioTags MatchTags = AudioTags.None;        
        
        //use enum bit comparison to check if tags match
        protected bool CompareAudioTag(Collider other)
        {
            if (MatchTags == AudioTags.None) return true;			
            var audioTag = other.GetComponent<AudioTag>();
            if (!audioTag) return false;
            var result = MatchTags & audioTag.Tags;
            return result != AudioTags.None;
        }

        protected GameObject GetEmitter(GameObject other)
        {
            return PostFrom == PostFrom.Self ? gameObject : other.gameObject;
        }

        public virtual void Activate(GameObject source = null)
        {
        }

        public virtual void Deactivate(GameObject source = null)
        {
        }

        protected override void Awake()
        {
            base.Awake();
            if (SetOn == TriggerCondition.AwakeDestroy)
                Activate(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (SetOn == TriggerCondition.AwakeDestroy)
                Deactivate(gameObject);
        }

        protected override void HandleEnableEvent()
        {            
            if (SetOn == TriggerCondition.EnableDisable)
                Activate(gameObject);
        }
        
        protected override void HandleDisableEvent()
        {            
            if (SetOn == TriggerCondition.EnableDisable)
                Deactivate(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (SetOn == TriggerCondition.TriggerEnterExit && CompareAudioTag(other))
                Activate(GetEmitter(other.gameObject));                         
        }

        private void OnTriggerExit(Collider other)
        {
            if (SetOn == TriggerCondition.TriggerEnterExit && CompareAudioTag(other))
                Deactivate(GetEmitter(other.gameObject));                                          
        }      
        
        private void OnCollisionEnter(Collision other)
        {
            if (SetOn == TriggerCondition.CollisionEnterExit && CompareAudioTag(other.collider))
                Activate(GetEmitter(other.gameObject));                         
        }

        private void OnCollisionExit(Collision other)
        {
            if (SetOn == TriggerCondition.CollisionEnterExit && CompareAudioTag(other.collider))
                Deactivate(GetEmitter(other.gameObject));                        
        }
    }
}