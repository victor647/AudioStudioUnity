using UnityEngine;

namespace AudioStudio.Components
{
    //sound emitter when dealing with collision
    public enum PostFrom
    {
        Self,
        Other
    }
    
    //define when the event is triggered
    public enum TriggerCondition
    {
        EnableDisable,
        TriggerEnterExit,
        CollisionEnterExit,        
        ManuallyControl
    }
    
    public abstract class AudioOnOffHandler : AsComponent
    {
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
        private void Start()
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
    }

    public abstract class AudioPhysicsHandler : AudioOnOffHandler
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
    }
}