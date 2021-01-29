using System.Collections.Generic;
using AudioStudio.Components;
using UnityEngine;

namespace AudioStudio
{
	/// <summary>
	/// Manage listener assignments and positions.
	/// </summary>
	internal static class ListenerManager
	{
		private static readonly List<AudioListener3D> _listeners = new List<AudioListener3D>();

		internal static void AssignAudioListener(AudioListener3D listener)
		{
			if (!_listeners.Contains(listener))
				_listeners.Add(listener);
		}
		
		internal static void RemoveAudioListener(AudioListener3D listener)
		{
			if (_listeners.Contains(listener))
				_listeners.Remove(listener);
		}
		
		internal static void UpdateListenerPositions()
		{
			if (_listeners.Count == 0) return;
			var activeListener = _listeners[_listeners.Count - 1];
			GlobalAudioEmitter.GameObject.transform.position = activeListener.Position;
			GlobalAudioEmitter.GameObject.transform.rotation = activeListener.transform.rotation;
		}

		internal static float GetListenerDistance(GameObject emitter)
		{
			return (emitter.transform.position - GlobalAudioEmitter.GameObject.transform.position).magnitude;
		}
	}
}