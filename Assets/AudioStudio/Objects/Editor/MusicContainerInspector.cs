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
	
		public override void OnInspectorGUI() 
		{
			serializedObject.Update();		
			AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Platform"));			
			DrawChildEvents();
			DrawAudioControls(_musicContainer);			
			DrawTransition(_musicContainer);
			serializedObject.ApplyModifiedProperties();
			DrawAuditionButtons(_musicContainer);
			AsGuiDrawer.DrawSaveButton(_musicContainer);
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
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Volume"));
				if (mc.Platform != Platform.Web)
				{
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Pitch"));
					AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Pan"));
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
	            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("PlayLogic"));
                switch (_musicContainer.PlayLogic)
                {                                                                    
                    case MusicPlayLogic.Random:
	                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AvoidRepeat"), "  Avoid Repeat", 120);     
	                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("RandomOnLoop"), "  Random On Loop", 120);
	                    EditorGUILayout.LabelField("Music Containers/Clips");
                        AsGuiDrawer.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);		                    
                        break;
	                case MusicPlayLogic.Layer:
                    case MusicPlayLogic.SequenceStep:
	                case MusicPlayLogic.SequenceContinuous:
                        EditorGUILayout.LabelField("Music Containers/Clips");
                        AsGuiDrawer.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);					
                        break;
                    case MusicPlayLogic.Switch:
	                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SwitchToSamePosition"), "  To Same Position", 120);
	                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SwitchImmediately"), "  Switch Immediately", 120);	                    
	                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("CrossFadeTime"), "  Cross Fade Time", 120);
	                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AudioSwitchReference"), "Audio Switch");
                        EditorGUILayout.LabelField("Switch Assignment");
                        AsGuiDrawer.DrawList(serializedObject.FindProperty("SwitchEventMappings"));
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
				EditorGUILayout.LabelField("Transition Exit Conditions");
				AsGuiDrawer.DrawList(serializedObject.FindProperty("TransitionExitConditions"));
				EditorGUILayout.LabelField("Transition Entry Conditions");
				AsGuiDrawer.DrawList(serializedObject.FindProperty("TransitionEntryConditions"));
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
	
	[CustomPropertyDrawer(typeof(TransitionExitData))]
    public class TransitionExitDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
	        var fullWidth = position.width;

	        position.width = fullWidth * 0.3f;
	        EditorGUI.LabelField(position, "Target");

	        position.x += position.width;
	        position.width = fullWidth * 0.7f;
	        EditorGUI.PropertyField(position, property.FindPropertyRelative("Target"), GUIContent.none);
	        GUILayout.EndHorizontal();

	        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fade Out", GUILayout.Width(60));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("FadeOutTime"), GUIContent.none, GUILayout.MinWidth(40));
            EditorGUILayout.LabelField("Exit Offset", GUILayout.Width(70));
            EditorGUILayout.PropertyField(property.FindPropertyRelative("ExitOffset"), GUIContent.none, GUILayout.MinWidth(40));
            GUILayout.EndHorizontal();
            
            AsGuiDrawer.DrawProperty(property.FindPropertyRelative("Interval"));
            AsGuiDrawer.DrawProperty(property.FindPropertyRelative("GridLength"));
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(" ");
        }
    }
    
    [CustomPropertyDrawer(typeof(TransitionEntryData))]
    public class TransitionEntryDataDrawer : PropertyDrawer
    {
	    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	    {
		    var fullWidth = position.width;

		    position.width = fullWidth * 0.3f;
		    EditorGUI.LabelField(position, "Source");

		    position.x += position.width;
		    position.width = fullWidth * 0.7f;
		    EditorGUI.PropertyField(position, property.FindPropertyRelative("Source"), GUIContent.none);
		    GUILayout.EndHorizontal();

		    GUILayout.BeginHorizontal();
		    EditorGUILayout.LabelField("Fade In", GUILayout.Width(60));
		    EditorGUILayout.PropertyField(property.FindPropertyRelative("FadeInTime"), GUIContent.none, GUILayout.MinWidth(40));
		    EditorGUILayout.LabelField("Entry Offset", GUILayout.Width(75));
		    EditorGUILayout.PropertyField(property.FindPropertyRelative("EntryOffset"), GUIContent.none, GUILayout.MinWidth(40));
		    GUILayout.EndHorizontal();
            
		    AsGuiDrawer.DrawProperty(property.FindPropertyRelative("TransitionSegment"), "Segment", 80);
		    GUILayout.BeginHorizontal();
		    EditorGUILayout.LabelField(" ");
	    }
    }
}