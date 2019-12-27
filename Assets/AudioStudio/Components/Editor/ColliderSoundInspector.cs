
using System.Linq;
using UnityEditor;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEngine;

namespace AudioStudio.Editor
{
	[CustomEditor(typeof(ColliderSound)), CanEditMultipleObjects]
	public class ColliderSoundInspector : AsComponentInspector
	{
		private ColliderSound _component;

		private void OnEnable()
		{
			_component = target as ColliderSound;
			CheckXmlExistence(_component);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			ShowSpatialSettings();
			EditorGUILayout.Separator();
			EditorGUILayout.LabelField("Trigger Settings", EditorStyles.boldLabel);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("PostFrom"), new GUIContent("Emitter"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("MatchTags"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("CollisionForceParameter"));
				if (_component.CollisionForceParameter.IsValid())
					EditorGUILayout.PropertyField(serializedObject.FindProperty("ValueScale"));
			}
			AsGuiDrawer.DrawList(serializedObject.FindProperty("EnterEvents"), "On Collision/Trigger Enter:", AddEnterEvent);
			AsGuiDrawer.DrawList(serializedObject.FindProperty("ExitEvents"), "On Collision/Trigger Exit:", AddExitEvent);

			serializedObject.ApplyModifiedProperties();
			AsGuiDrawer.CheckLinkedComponent<Collider>(_component);
			ShowButtons(_component);
		}

		private void AddEnterEvent(Object[] objects)
		{
			var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
			foreach (var evt in events)
			{
				AsScriptingHelper.AddToArray(ref _component.EnterEvents, new PostEventReference(evt.name));
			}
		}

		private void AddExitEvent(Object[] objects)
		{
			var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
			foreach (var evt in events)
			{
				AsScriptingHelper.AddToArray(ref _component.ExitEvents, new PostEventReference(evt.name));
			}
		}

		protected override void Refresh()
		{
			foreach (var evt in _component.EnterEvents)
			{
				AsComponentBackup.RefreshEvent(evt);
			}

			foreach (var evt in _component.ExitEvents)
			{
				AsComponentBackup.RefreshEvent(evt);
			}

			AsComponentBackup.RefreshParameter(_component.CollisionForceParameter);
		}
	}
}