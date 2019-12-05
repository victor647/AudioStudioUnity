using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
	[AddComponentMenu("AudioStudio/AudioListener3D")]
	[DisallowMultipleComponent]
	public class AudioListener3D : AudioOnOffHandler
	{
		public Vector3 PositionOffset = Vector3.zero;
		

		protected override void HandleEnableEvent()
		{
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Listener, AudioAction.Activate, AudioTriggerSource.AudioListener3D, "Add 3D Listener", gameObject);
			ListenerManager.AssignAudioListener(this);
		}

		protected override void HandleDisableEvent()
		{			
			AsUnityHelper.DebugToProfiler(Severity.Notification, AudioObjectType.Listener, AudioAction.Deactivate, AudioTriggerSource.AudioListener3D, "Remove 3D Listener", gameObject);				
			ListenerManager.RemoveAudioListener(this);
		}

		public Vector3 Position => transform.position + PositionOffset;
	}
}