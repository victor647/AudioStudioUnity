using System.Linq;
using UnityEngine;


namespace AudioStudio
{    
    public class AudioState : StateMachineBehaviour
    {
        private AnimationSound _animationSound;
        public AudioEventReference[] EnterEvents = new AudioEventReference[0];
        public AudioEventReference[] ExitEvents = new AudioEventReference[0];
        public bool StopEventsOnExit;
        public bool ResetStateOnExit = true;
        public AnimationAudioState AnimationAudioState = AnimationAudioState.None;


        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
        {
            if (!_animationSound)
            {
                _animationSound = animator.gameObject.GetComponent<AnimationSound>();
                if (!_animationSound) return;
            }
            if (_animationSound)                
            {                
                foreach (var evt in EnterEvents)
                {
                    evt.Post(_animationSound.gameObject);                                    
                }
            }

            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.AudioState, AudioAction.Activate, "Enter State", animator.gameObject.name, AnimationAudioState.ToString());
            _animationSound.SetAnimationState(AnimationAudioState);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
        {
            if (_animationSound )
            {
                if (StopEventsOnExit)
                {
                    foreach (var evt in EnterEvents)
                    {
                        evt.Stop(_animationSound.gameObject);  
                    }
                }
                foreach (var evt in ExitEvents)
                {
                    evt.Post(_animationSound.gameObject);               
                }
            }
            if (ResetStateOnExit && _animationSound) _animationSound.SetAnimationState(AnimationAudioState.None);
            AudioManager.DebugToProfiler(MessageType.Component, ObjectType.AudioState, AudioAction.Deactivate, "Exit State", animator.gameObject.name,  AnimationAudioState.ToString());
        }
        
        public bool IsValid()
        {
            return EnterEvents.Any(s => s.IsValid()) || ExitEvents.Any(s => s.IsValid()) || AnimationAudioState != AnimationAudioState.None;
        }
    }
}
