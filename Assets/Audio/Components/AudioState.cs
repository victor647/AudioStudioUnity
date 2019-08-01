using System.Linq;
using AudioStudio.Configs;
using UnityEngine;


namespace AudioStudio.Components
{    
    public class AudioState : StateMachineBehaviour
    {
        public AudioEventReference[] EnterEvents = new AudioEventReference[0];
        public AudioEventReference[] ExitEvents = new AudioEventReference[0];
        public SetSwitchReference[] EnterSwitches = new SetSwitchReference[0];
        public bool StopEventsOnExit;
        public bool ResetStateOnExit = true;
        public AnimationAudioState AnimationAudioState = AnimationAudioState.None;
        
        private AnimationSound _animationSound;


        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
        {
            if (!_animationSound)
                _animationSound = animator.gameObject.GetComponent<AnimationSound>();
            
            foreach (var swc in EnterSwitches)
            {
                swc.SetValue(animator.gameObject);             
            }
            foreach (var evt in EnterEvents)
            {
                evt.Post(animator.gameObject);
            }   

            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.AudioState, AudioAction.Activate, "Enter State", animator.gameObject.name, AnimationAudioState.ToString());
            _animationSound.SetAnimationState(AnimationAudioState);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
        {
            if (StopEventsOnExit)
            {
                foreach (var evt in EnterEvents)
                {
                    evt.Stop(animator.gameObject);  
                }
            }
            foreach (var evt in ExitEvents)
            {
                evt.Post(animator.gameObject);               
            }
            AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.AudioState, AudioAction.Deactivate, "Exit State", animator.gameObject.name,  AnimationAudioState.ToString());
            if (ResetStateOnExit && _animationSound)
            {
                _animationSound.SetAnimationState(AnimationAudioState.None);
                AudioManager.DebugToProfiler(ProfilerMessageType.Component, ObjectType.AudioState, AudioAction.Deactivate, "Reset State", animator.gameObject.name,  "None");
            }    
        }
        
        public bool IsValid()
        {
            return EnterEvents.Any(s => s.IsValid()) || ExitEvents.Any(s => s.IsValid()) || AnimationAudioState != AnimationAudioState.None;
        }
    }
}
