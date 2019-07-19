using UnityEngine;

namespace AudioStudio
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
    }
    
    public abstract class AudioOnOffHandler : AsComponent
    {
        private bool _started; 

        private void OnEnable()
        {                                  
            if (_started) HandleEnableEvent();                                                                       
        }

        //make sure the first time it is played at Start instead of OnEnable
        private void Start()
        {
            _started = true;
            HandleEnableEvent();  
        }

        private void OnDisable()
        {                                  
            if (_started) HandleDisableEvent();            
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
    }
}