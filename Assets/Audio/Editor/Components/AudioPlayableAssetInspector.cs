using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(AudioPlayableAsset)), CanEditMultipleObjects]
	public class AudioPlayableAssetInspector : AsComponentInspector
	{

		private AudioPlayableAsset _component;

		private void OnEnable()
		{
			_component = target as AudioPlayableAsset;
			CheckXmlExistence();
		}
		
		private void CheckXmlExistence()
		{
			var path = AssetDatabase.GetAssetPath(_component);
			var clip = AsTimelineAudioBackup.GetClipFromComponent(_component);
			BackedUp = AsTimelineAudioBackup.Instance.FindComponentNode(path, clip) != null;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("StopOnEnd"));
			AudioScriptGUI.DrawList(serializedObject.FindProperty("StartEvents"), "Start Events:", AddStartEvent);
			AudioScriptGUI.DrawList(serializedObject.FindProperty("EndEvents"), "End Events:", AddEndEvent);
			serializedObject.ApplyModifiedProperties();
			if (GUILayout.Button("Rename Clip By Event"))
			{
				var clip = AsTimelineAudioBackup.GetClipFromComponent(_component);
				if (_component.StartEvents.Length > 0)
					clip.displayName = _component.StartEvents[0].Name;
				else if (_component.EndEvents.Length > 0)
					clip.displayName = _component.EndEvents[0].Name;
			}

			ShowButtons(_component);
		}

		private void AddStartEvent(Object[] objects)
		{
			var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
			foreach (var evt in events)
			{
				AudioUtility.AddToArray(ref _component.StartEvents, new AudioEventReference(evt.name));
			}
		}

		private void AddEndEvent(Object[] objects)
		{
			var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
			foreach (var evt in events)
			{
				AudioUtility.AddToArray(ref _component.EndEvents, new AudioEventReference(evt.name));
			}
		}

		protected override void Refresh()
		{
			foreach (var evt in _component.StartEvents)
			{
				AsComponentBackup.RefreshEvent(evt);
			}

			foreach (var evt in _component.EndEvents)
			{
				AsComponentBackup.RefreshEvent(evt);
			}
		}
		
		protected override void UpdateXml(Object component, XmlAction action)
		{
			var edited = false;
			var apa = (AudioPlayableAsset) component;
			var path = AssetDatabase.GetAssetPath(component);
			var clip = AsTimelineAudioBackup.GetClipFromComponent(apa);
			switch (action)
			{
				case XmlAction.Remove:
					AsTimelineAudioBackup.Instance.RemoveComponentNode(path, clip);
					DestroyImmediate(apa, true);
					break;
				case XmlAction.Save:
					edited = AsTimelineAudioBackup.Instance.UpdateComponentNode(path, clip, apa);
					break;
				case XmlAction.Revert:
					edited = AsTimelineAudioBackup.Instance.RevertComponentToXml(path, clip, apa);
					break;
			}
			BackedUp = true;
			if (edited) 
				AssetDatabase.SaveAssets();
		}
	}
}