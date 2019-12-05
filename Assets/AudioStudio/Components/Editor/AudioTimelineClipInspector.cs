using AudioStudio.Timeline;
using UnityEditor;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(AudioTimelineClip)), CanEditMultipleObjects]
	public class AudioTimelineClipInspector : AsComponentInspector
	{
		private AudioTimelineClip _component;

		private void OnEnable()
		{
			_component = target as AudioTimelineClip;
			CheckXmlExistence();
		}
		
		private void CheckXmlExistence()
		{
			var path = AssetDatabase.GetAssetPath(_component);
			var clip = AsTimelineAudioBackup.GetClipFromComponent(path, _component);
			BackedUp = AsTimelineAudioBackup.Instance.ComponentBackedUp(path, clip);
		}

		public override void OnInspectorGUI()
		{
			EditorGUI.indentLevel = 0;
			serializedObject.Update();
			
			DrawEmitter();
			AsGuiDrawer.DrawList(serializedObject.FindProperty("StartEvents"), "Start Events:");
			EditorGUILayout.PropertyField(serializedObject.FindProperty("StopOnEnd"));
			AsGuiDrawer.DrawList(serializedObject.FindProperty("EndEvents"), "End Events:");
			EditorGUILayout.Separator();
			AsGuiDrawer.DrawList(serializedObject.FindProperty("StartSwitches"), "Start Switches:");
			AsGuiDrawer.DrawList(serializedObject.FindProperty("EndSwitches"), "End Switches:");
			EditorGUILayout.PropertyField(serializedObject.FindProperty("GlobalSwitch"));

			serializedObject.ApplyModifiedProperties();
			ShowButtons(_component);
		}

		private void DrawEmitter()
		{
			var names = _component.GetEmitterNames();
			_component.EmitterIndex = EditorGUILayout.Popup("Emitter", _component.EmitterIndex, names);
		}

		protected override void Refresh()
		{
			base.Refresh();
			foreach (var evt in _component.StartEvents)
			{
				AsComponentBackup.RefreshEvent(evt);
			}

			foreach (var evt in _component.EndEvents)
			{
				AsComponentBackup.RefreshEvent(evt);
			}

			foreach (var swc in _component.StartSwitches)
			{
				AsComponentBackup.RefreshSwitch(swc);
			}

			foreach (var swc in _component.EndSwitches)
			{
				AsComponentBackup.RefreshSwitch(swc);
			}
		}

		protected override void UpdateXml(Object obj, XmlAction action)
		{
			var edited = false;
			var component = (AudioTimelineClip) obj;
			var assetPath = AssetDatabase.GetAssetPath(obj);
			var clip = AsTimelineAudioBackup.GetClipFromComponent(assetPath, component);
			switch (action)
			{
				case XmlAction.Remove:
					AsTimelineAudioBackup.Instance.RemoveComponentXml(assetPath, clip);
					DestroyImmediate(component, true);
					break;
				case XmlAction.Save:
					clip.displayName = _component.AutoRename();
					edited = AsTimelineAudioBackup.Instance.UpdateXmlFromComponent(assetPath, clip, component);
					break;
				case XmlAction.Revert:
					edited = AsTimelineAudioBackup.Instance.RevertComponentToXml(assetPath, clip, component);
					break;
			}
			BackedUp = true;
			if (edited) 
				AssetDatabase.SaveAssets();
		}
	}
}