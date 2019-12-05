using AudioStudio.Components;
using UnityEngine;

namespace AudioStudio
{
	public static class ListenerManager
	{
		private static AudioListener3D _currentFollowingListener;
		
		public static void Init()
		{
			GlobalAudioEmitter.GameObject.AddComponent<AudioListener>();
		}
		
		public static void AssignAudioListener(AudioListener3D listener)
		{
			_currentFollowingListener = listener;
		}
		
		public static void RemoveAudioListener(AudioListener3D listener)
		{
			if (_currentFollowingListener == listener)
				_currentFollowingListener = null;
		}
		
		public static void UpdateListenerPositions()
		{
			if (!_currentFollowingListener) return;
			GlobalAudioEmitter.GameObject.transform.position = _currentFollowingListener.Position;
			GlobalAudioEmitter.GameObject.transform.rotation = _currentFollowingListener.transform.rotation;
		}

		public static float GetListenerDistance(GameObject emitter)
		{
			return (emitter.transform.position - GlobalAudioEmitter.GameObject.transform.position).magnitude;
		}
	}
}