using AudioStudio.Components;
using UnityEngine;

namespace AudioStudio
{
	internal static class ListenerManager
	{
		private static AudioListener3D _currentFollowingListener;
		
		internal static void Init()
		{
			GlobalAudioEmitter.GameObject.AddComponent<AudioListener>();
		}
		
		internal static void AssignAudioListener(AudioListener3D listener)
		{
			_currentFollowingListener = listener;
		}
		
		internal static void RemoveAudioListener(AudioListener3D listener)
		{
			if (_currentFollowingListener == listener)
				_currentFollowingListener = null;
		}
		
		internal static void UpdateListenerPositions()
		{
			if (!_currentFollowingListener) return;
			GlobalAudioEmitter.GameObject.transform.position = _currentFollowingListener.Position;
			GlobalAudioEmitter.GameObject.transform.rotation = _currentFollowingListener.transform.rotation;
		}

		internal static float GetListenerDistance(GameObject emitter)
		{
			return (emitter.transform.position - GlobalAudioEmitter.GameObject.transform.position).magnitude;
		}
	}
}