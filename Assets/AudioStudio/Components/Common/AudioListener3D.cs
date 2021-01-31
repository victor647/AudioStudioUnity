using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Components
{
	[AddComponentMenu("AudioStudio/Audio Listener 3D")]
	[DisallowMultipleComponent]
	public class AudioListener3D : AsComponent
	{
		public Vector3 PositionOffset = Vector3.zero;
		public bool MoveZAxisByCameraFOV;
		public float MinFOV;
		public float MaxFOV;
		public float MinOffset;
		public float MaxOffset;
		private Camera _camera;

		protected override void Start()
		{
			base.Start();
			_camera = GetComponent<Camera>();
		}
		

		protected override void HandleEnableEvent()
		{
			AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Listener, AudioAction.Activate, AudioTriggerSource.AudioListener3D, "Add 3D Listener", gameObject);
			ListenerManager.AssignAudioListener(this);
		}

		protected override void HandleDisableEvent()
		{			
			AsUnityHelper.AddLogEntry(Severity.Notification, AudioObjectType.Listener, AudioAction.Deactivate, AudioTriggerSource.AudioListener3D, "Remove 3D Listener", gameObject);				
			ListenerManager.RemoveAudioListener(this);
		}

		public Vector3 Position
		{
			get
			{
				if (MoveZAxisByCameraFOV && _camera)
				{
					var offsetZ = ParameterMapping.ConvertParameterToTarget(_camera.fieldOfView, MinFOV, MaxFOV, MinOffset, MaxOffset);
					return transform.position + transform.rotation * (PositionOffset + new Vector3(0, 0, offsetZ));
				}
				return transform.position + transform.rotation * PositionOffset;
			}
		}
	}
}