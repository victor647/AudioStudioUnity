using AudioStudio.Components;
using AudioStudio.Tools;
using UnityEngine;
using UnityEngine.Playables;

namespace AudioStudio.Timeline
{
    public class AudioTimelineComponent : PlayableBehaviour
    {
        private GameObject _emitter;
        private AudioTimelineClip _component;
        private bool _started;
        private PlayableDirector _director;
        private double _endTime;

        public void Init(AudioTimelineClip component, GameObject emitter, GameObject director)
        {
            _component = component;
            _director = director.GetComponent<PlayableDirector>();
            _endTime = component.EndTime;
            if (emitter)
            {
                var ago = emitter.GetComponent<AudioEmitterObject>();
                _emitter = ago ? ago.GetSoundSource() : emitter.gameObject;
            }
            else
            {
                var ago = director.GetComponent<AudioEmitterObject>();
                _emitter = ago ? ago.GetSoundSource() : GlobalAudioEmitter.GameObject;
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying || !_emitter || _started) return;
            _started = true;
            ProcessStartActions();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!Application.isPlaying || !_emitter || !IsClipFinished()) return;
            ProcessEndActions();
        }
        
        private bool IsClipFinished()
        {
            if (!_director) return true;
            if (_director.time >= _endTime ||_director.time == 0 && _started)
            {
                _started = false;
                return true;
            }
            return false;
        }

        private void ProcessStartActions()
        {
            foreach (var swc in _component.StartSwitches)
            {
                swc.SetValue(_component.GlobalSwitch ? null : _emitter, AudioTriggerSource.AudioTimelineClip);
            }
            
            foreach (var evt in _component.StartEvents)
            {
                evt.Post(_emitter, -1f, AudioTriggerSource.AudioTimelineClip);
            }
        }
        
        private void ProcessEndActions()
        {
            if (_component.StopOnEnd)
            {
                foreach (var evt in _component.StartEvents)
                {
                    evt.Stop(_emitter, -1f, AudioTriggerSource.AudioTimelineClip);
                }
            }

            foreach (var swc in _component.EndSwitches)
            {
                swc.SetValue(_component.GlobalSwitch ? null : _emitter, AudioTriggerSource.AudioTimelineClip);
            }
            
            foreach (var evt in _component.EndEvents)
            {
                evt.Post(_emitter, -1f, AudioTriggerSource.AudioTimelineClip);
            }
        }
    }
}