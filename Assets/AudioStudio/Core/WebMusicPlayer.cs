#if !UNITY_EDITOR && UNITY_WEBGL
using System.Collections;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio
{
	public class WebMusicInstance
		{
			public MusicTrack Track;
			private WebGLStreamingAudioSourceInterop _interopA;
			private WebGLStreamingAudioSourceInterop _interopB;
			private WebGLStreamingAudioSourceInterop _activeInterop;
			private float _LoopDuration;
            public float LoopDuration
            {
                get
                {
                    CheckLoopDuration();        // 第一次从js中取duration可能为NaN，所以每次都从js中获取一次
                    return _LoopDuration;
                }
            }
			public float FadeInTime;
			public float FadeOutTime;			
			public bool Loop;

			public WebMusicInstance(MusicTrack track, float fadeInTime, float fadeOutTime)
			{
				FadeInTime = fadeInTime;
				FadeOutTime = fadeOutTime;
				Track = track;				
				Loop = Track.LoopCount == 0;
				var go = WebMusicPlayer.Instance.gameObject;
				//add the prefix back to get the corresponding audio file
				var clipName = "Music_" + track.name;
				_interopA = new WebGLStreamingAudioSourceInterop(AsAssetLoader.GetClipUrl(clipName, AudioObjectType.Music), go);

				//if in simple loop mode or not looping
				if (track.UseDefaultLoopStyle || !Loop)
				{
                    _LoopDuration = _interopA.Duration - 0.01f;						
				}
				else //calculate the loop duration by rhythm
				{
					_interopB = new WebGLStreamingAudioSourceInterop(AsAssetLoader.GetClipUrl(clipName, AudioObjectType.Music), go);
					_LoopDuration = track.LoopDurationRealTime();
				}				
			}

            private void CheckLoopDuration()
            {
                if (Track.UseDefaultLoopStyle || !Loop)
                {
                    _LoopDuration = _interopA.Duration - 0.01f;
                }
            }

			public void Play()
			{
				//switch to the idle audio source to play again
				if (WebMusicPlayer.CurrentPlaying.Loop && !WebMusicPlayer.CurrentPlaying.Track.UseDefaultLoopStyle && _activeInterop == _interopA)
				{										
					_interopB.CurrentTime = 0f;
					_interopB.Play();
					_activeInterop = _interopB;					
				}
				else //use the same audio source
				{
					_interopA.CurrentTime = 0f;
					_interopA.Play();
					_activeInterop = _interopA;
				}
                Volume = Track.Volume;
            }

			public float Volume
			{
				get
				{
					return _activeInterop.Volume;
				}
				set
				{
					_activeInterop.Volume = WebMusicPlayer.Instance.IsMuted ? 0 : Mathf.Clamp01(value);
				}
            }

			public void Stop()
			{
				_interopA?.Pause();
				_interopB?.Pause();
				_activeInterop = null;				
			}
		}		
	
	public class WebMusicPlayer : MonoBehaviour
	{
		public static WebMusicInstance CurrentPlaying;

		private static WebMusicPlayer _instance;
		public static WebMusicPlayer Instance
		{
			get
			{
				if (!_instance)
				{
					var go = new GameObject("Web Music Player");
					_instance = go.AddComponent<WebMusicPlayer>();
					DontDestroyOnLoad(_instance);
					_instance.IsMuted = !AudioManager.MusicEnabled;
				}
				return _instance;
			}
		}	

        private bool _isMuted;
        public bool IsMuted
        {
            get { return _isMuted; }
            set
            {
                _isMuted = value;
                if(CurrentPlaying != null)
                    CurrentPlaying.Volume = CurrentPlaying.Track.Volume;
            }
        }

		public void StopMusic()
		{
			if (CurrentPlaying == null) return;
			CurrentPlaying.Stop();
			CurrentPlaying = null;
			CancelInvoke(nameof(Loop));
		}

		public void PlayMusic(WebMusicInstance music)
		{
			if (CurrentPlaying == null)
			{
				CurrentPlaying = music;
                CurrentPlaying.Play();
                CurrentPlaying.Volume = music.Track.Volume;
                Invoke(nameof(Loop), music.LoopDuration);
			}
			else
			{
				CancelInvoke(nameof(Loop));
				StartCoroutine(CrossFade(music));
			}				
		}
		
		private IEnumerator CrossFade(WebMusicInstance newMusic)
		{
			if (CurrentPlaying.FadeOutTime > 0f)                
			{
                var fadeOutSteps = Mathf.FloorToInt(CurrentPlaying.FadeOutTime * 20);
                var fadeOutVolumeStep = CurrentPlaying.Volume / fadeOutSteps;
                var fadeOutTimeStep = CurrentPlaying.FadeOutTime / fadeOutSteps;

                CurrentPlaying.Volume = newMusic.Track.Volume;
                for (var i = 0; i < fadeOutSteps; i++)
                {
                    CurrentPlaying.Volume -= fadeOutVolumeStep;
                    yield return new WaitForSecondsRealtime(fadeOutTimeStep);
                }
            }		
			CurrentPlaying.Stop();
			CurrentPlaying = newMusic;		
			CurrentPlaying.Play();
            Invoke(nameof(Loop), newMusic.LoopDuration);
            var targetVolume = newMusic.Track.Volume;

            if (newMusic.FadeInTime > 0f)
            {
                var fadeInSteps = Mathf.FloorToInt(CurrentPlaying.FadeInTime * 20);                
                var fadeInVolumeStep = targetVolume / fadeInSteps;
                var fadeInTimeStep = CurrentPlaying.FadeInTime / fadeInSteps;

                CurrentPlaying.Volume = 0f;
                for (var i = 0; i < fadeInSteps; i++)
                {
                    CurrentPlaying.Volume += fadeInVolumeStep;
                    yield return new WaitForSecondsRealtime(fadeInTimeStep);
                }
            }								
            CurrentPlaying.Volume = targetVolume;
        }

		private void Loop()
		{
			//if the music playing should not loop
			if (!CurrentPlaying.Loop)
			{
				CurrentPlaying.Stop();
				CurrentPlaying = null;
			}
			else
			{
				CurrentPlaying.Play();
				Invoke(nameof(Loop), CurrentPlaying.LoopDuration);
			}	
		}
	}
}
#endif