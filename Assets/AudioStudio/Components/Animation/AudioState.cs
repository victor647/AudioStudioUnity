using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;


namespace AudioStudio.Components
{    
    public class AudioState : StateMachineBehaviour
    {
        public PostEventReference[] EnterEvents = new PostEventReference[0];
        public PostEventReference[] ExitEvents = new PostEventReference[0];
        public SetSwitchReference[] EnterSwitches = new SetSwitchReference[0];
        public bool ResetStateOnExit = true;
        public AnimationAudioState AnimationAudioState = AnimationAudioState.None;
        
        private AnimationSound _animationSound;


        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
        {
            if (!_animationSound)
                _animationSound = AsUnityHelper.GetOrAddComponent<AnimationSound>(animator.gameObject);
            
            foreach (var swc in EnterSwitches)
            {
                swc.SetValue(animator.gameObject, AudioTriggerSource.AudioState);             
            }
            foreach (var evt in EnterEvents)
            {
                evt.Post(animator.gameObject, AudioTriggerSource.AudioState);
            }
            _animationSound.SetAnimationState(AnimationAudioState);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,int layerIndex)
        {
            foreach (var evt in ExitEvents)
            {
                evt.Post(animator.gameObject, AudioTriggerSource.AudioState);               
            }
            if (ResetStateOnExit && _animationSound)
                _animationSound.SetAnimationState(AnimationAudioState.None);
        }
        
        public bool IsValid()
        {
            return EnterEvents.Any(s => s.IsValid()) || ExitEvents.Any(s => s.IsValid()) || AnimationAudioState != AnimationAudioState.None;
        }
    }
}
