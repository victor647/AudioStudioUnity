using System.Linq;
using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Editor
{        
	[CustomEditor(typeof(MusicContainer)), CanEditMultipleObjects]
	public class MusicContainerInspector : AudioEventInspector 
	{
		private MusicContainer _musicContainer;
	
		private void OnEnable()
		{
			_musicContainer = target as MusicContainer;
		}
	
		public override void OnInspectorGUI () {
		
			serializedObject.Update();		
			DrawHierarchy();
			DrawChildEvents();
			DrawAudioControls(_musicContainer);			
			DrawTransition(_musicContainer);
			AudioScriptGUI.DrawSaveButton(_musicContainer);
			serializedObject.ApplyModifiedProperties();
		}

		protected void DrawHierarchy()
		{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("Platform"));					
		}

		protected void DrawAudioControls(MusicContainer mc)
		{
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Audio Controls", EditorStyles.boldLabel, GUILayout.Width(150)); 
			if (!mc.IndependentEvent)
			{                    
				EditorGUILayout.LabelField("Override", GUILayout.Width(60));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideControls"), GUIContent.none);
				if (!mc.OverrideControls) GUI.enabled = false;
			}
			GUILayout.EndHorizontal();
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{				
				DrawProperty("Volume");
				if (mc.Platform != Platform.Web)
				{
					DrawProperty("Pitch");
					DrawProperty("Pan");
					DrawFilters(mc);
					DrawSubMixer(mc);
					DrawParameterMappings();
				}
				GUI.enabled = true;
			}
			EditorGUILayout.Separator();
		}
		
		private void DrawChildEvents()
		{			
			EditorGUILayout.LabelField("Child Events", EditorStyles.boldLabel);    
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {			
	            EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayLogic"));
                switch (_musicContainer.PlayLogic)
                {                                                                    
                    case MusicPlayLogic.Random:
	                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AvoidRepeat"));
	                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomOnLoop"));
                        EditorGUILayout.LabelField("Music Containers/Clips");
                        AudioScriptGUI.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);		                    
                        break;
	                case MusicPlayLogic.Layer:
                    case MusicPlayLogic.SequenceStep:
	                case MusicPlayLogic.SequenceContinuous:
                        EditorGUILayout.LabelField("Music Containers/Clips");
                        AudioScriptGUI.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);					
                        break;
                    case MusicPlayLogic.Switch:	                    	                    
	                    DrawProperty("AudioSwitchReference", "Switch", 100);
	                    DrawProperty("SwitchToSamePosition", "To Same Position");
	                    DrawProperty("SwitchImmediately", "Switch Immediately");	                    
	                    DrawProperty("CrossFadeTime", "Cross Fade Time", 116, 50);		                    	                                        	                    	                    	                    	                    		
                        EditorGUILayout.LabelField("Switch Assignment");
                        AudioScriptGUI.DrawList(serializedObject.FindProperty("SwitchEventMappings"));
                        break;
                }					
            }
			EditorGUILayout.Separator();
        }
		
		protected void DrawTransition(MusicContainer mc)
		{			
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Transition Settings", EditorStyles.boldLabel, GUILayout.Width(150)); 
			if (!mc.IndependentEvent)
			{                    
				EditorGUILayout.LabelField("Override", GUILayout.Width(60));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideTransition"), GUIContent.none);
				if (!mc.OverrideTransition) 
					GUI.enabled = false;
			}
			GUILayout.EndHorizontal();
			using (new GUILayout.VerticalScope(GUI.skin.box))
			{
				if (mc.Platform != Platform.Web)
				{
					DrawProperty("TransitionInterval", "Exit at", 80);
					if (mc.TransitionInterval == TransitionInterval.NextGrid)
					{
						DrawProperty("GridLength", "Grid Length", 80);
					}
				}

				GUILayout.BeginHorizontal();				
				EditorGUILayout.LabelField("Fade In", GUILayout.MaxWidth(80));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultFadeInTime"), GUIContent.none,
					GUILayout.Width(40));
				EditorGUILayout.LabelField("Fade Out", GUILayout.MaxWidth(80));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultFadeOutTime"), GUIContent.none,
					GUILayout.Width(40));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Entry Offset", GUILayout.MaxWidth(80));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultEntryOffset"), GUIContent.none,
					GUILayout.Width(40));
				EditorGUILayout.LabelField("Exit Offset", GUILayout.MaxWidth(80));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultExitOffset"), GUIContent.none,
					GUILayout.Width(40));
				GUILayout.EndHorizontal();								
				GUI.enabled = true;
			}
			EditorGUILayout.Separator();
		}
		
		private void AddChildEvent(Object[] objects)
		{
			var events = objects.Select(obj => obj as MusicContainer).Where(a => a).ToArray();                   
			foreach (var evt in events)
			{
				_musicContainer.ChildEvents.Add(evt);
			}								
		}
	}   
}