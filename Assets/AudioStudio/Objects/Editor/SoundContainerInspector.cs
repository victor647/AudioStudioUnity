using System.Linq;
using AudioStudio.Configs;
using UnityEngine;
using UnityEditor;

namespace AudioStudio.Editor
{	
    [CustomEditor(typeof(SoundContainer)), CanEditMultipleObjects]
    public class SoundContainerInspector : AudioEventInspector
    {	
        private SoundContainer _soundContainer;

        private void OnEnable()
        {
            _soundContainer = target as SoundContainer;
        }
	
        public override void OnInspectorGUI()
        {
            serializedObject.Update();		 
            DrawHierarchy(_soundContainer);
            Draw3DSetting(_soundContainer);		
            DrawPlayLogic();
            DrawChildEvents();		
            DrawAudioControls(_soundContainer);
            DrawVoiceManagement(_soundContainer);
            serializedObject.ApplyModifiedProperties();
            DrawAuditionButtons(_soundContainer);
            AsGuiDrawer.DrawSaveButton(_soundContainer);
        }       	                
        
        protected virtual void DrawPlayLogic()
        {
        }
		
        protected virtual void DrawChildEvents()
        {
            AsGuiDrawer.DrawList(serializedObject.FindProperty("ChildEvents"), "Child Sound Events", AddChildEvent);
            EditorGUILayout.Separator();
        }
        
        private void AddChildEvent(Object[] objects)
        {
            var events = objects.Select(obj => obj as SoundContainer).Where(a => a).ToArray();                   
            foreach (var evt in events)
            {
                _soundContainer.ChildEvents.Add(evt);
            }								
        }
        
        protected void DrawAudioControls(SoundContainer sc)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Audio Controls", EditorStyles.boldLabel, GUILayout.Width(150)); 
            if (!sc.IndependentEvent)
            {                    
                EditorGUILayout.LabelField("Override", GUILayout.Width(60));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideControls"), GUIContent.none);
                if (!sc.OverrideControls) GUI.enabled = false;
            }
            GUILayout.EndHorizontal();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
	            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Probability"));
                GUILayout.BeginHorizontal();
                if (sc.RandomizeVolume)
                {
                    EditorGUILayout.LabelField("Volume", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Volume"), GUIContent.none,GUILayout.Width(40));                
                    EditorGUILayout.LabelField("Random", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizeVolume"), GUIContent.none, GUILayout.Width(15));   
                    EditorGUILayout.LabelField("Range", GUILayout.Width(40));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("VolumeRandomRange"), GUIContent.none, GUILayout.Width(35));                    
                }
                else
                {
                    EditorGUILayout.LabelField("Volume", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Volume"), GUIContent.none,GUILayout.MinWidth(50));                
                    EditorGUILayout.LabelField("Random", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizeVolume"), GUIContent.none);
                }
                GUILayout.EndHorizontal();
		
                GUILayout.BeginHorizontal();                                             
                if (sc.RandomizePitch)
                {
                    EditorGUILayout.LabelField("Pitch", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Pitch"), GUIContent.none,GUILayout.Width(40));                
                    EditorGUILayout.LabelField("Random", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizePitch"), GUIContent.none, GUILayout.Width(15));   
                    EditorGUILayout.LabelField("Range", GUILayout.Width(40));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("PitchRandomRange"), GUIContent.none, GUILayout.Width(35));
                }
                else
                {
                    EditorGUILayout.LabelField("Pitch", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Pitch"), GUIContent.none,GUILayout.MinWidth(50));                
                    EditorGUILayout.LabelField("Random", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizePitch"), GUIContent.none);
                }
                GUILayout.EndHorizontal();                                                
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Pan"));                                                
                DrawFilters(sc);   
                DrawSubMixer(sc);
                DrawParameterMappings();
                GUI.enabled = true;
            }
            EditorGUILayout.Separator();            
        }

        protected void DrawVoiceManagement(SoundContainer sc)
        {
	        if (!sc.IndependentEvent && !sc.EnableVoiceLimit)
		        return;
	        
	        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Voice Management", EditorStyles.boldLabel, GUILayout.Width(150));
            if (sc.IndependentEvent)
	            EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableVoiceLimit"), GUIContent.none);
            else
	            GUI.enabled = false;
            GUILayout.EndHorizontal();

            if (sc.EnableVoiceLimit)
            {
	            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
	            {
		            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("VoiceLimiter"));
		            if (sc.VoiceLimiter)
			            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Priority"));
		            EditorGUILayout.LabelField("Max Voices Allowed ");
		            GUILayout.BeginHorizontal();
		            EditorGUILayout.LabelField("Global: ", GUILayout.Width(80));
		            EditorGUILayout.PropertyField(serializedObject.FindProperty("VoiceLimitGlobal"), GUIContent.none, GUILayout.Width(30));
		            EditorGUILayout.LabelField("GameObject: ", GUILayout.Width(80));
		            EditorGUILayout.PropertyField(serializedObject.FindProperty("VoiceLimitGameObject"), GUIContent.none, GUILayout.Width(30));
		            GUILayout.EndHorizontal();
	            }
            }

            GUI.enabled = true;
            EditorGUILayout.Separator();
        }
        
        protected void DrawHierarchy(SoundContainer sc)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Event Hierarchy", EditorStyles.boldLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField("Independent", GUILayout.Width(80));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IndependentEvent"), GUIContent.none);
            GUILayout.EndHorizontal();
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                if (Selection.objects.Length > 1)
                    EditorGUILayout.HelpBox("Unavailable when selecting multiple events", MessageType.Info);
                else
                    DrawCascade(sc.GetParentContainer());
            }
            EditorGUILayout.Separator();
        }

        private void DrawCascade(SoundContainer sc, int indentLevel = 0)
        {
            if (!sc) return;
            DrawCascadeItem(sc, indentLevel);
            foreach (var childEvent in sc.ChildEvents)
            {
                DrawCascade(childEvent, indentLevel + 1);
            }
        }
        
        protected void Draw3DSetting(SoundContainer sc)
        {
	        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("3D Settings", EditorStyles.boldLabel, GUILayout.Width(100)); 
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Is3D"), GUIContent.none, GUILayout.Width(20));
            
            if (!sc.IndependentEvent)
            {                    
                EditorGUILayout.LabelField("Override", GUILayout.Width(60));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideSpatial"), GUIContent.none);
            }
            GUILayout.EndHorizontal();
            
            if (!sc.IndependentEvent && !sc.Is3D)
	            return;
            
            if (!sc.IndependentEvent && !sc.OverrideSpatial) 
	            GUI.enabled = false;

            if (sc.Is3D)
            {
	            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
	            {
		            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SpatialBlend"));
		            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SpreadWidth"));
		            
		            GUILayout.BeginHorizontal();
		            EditorGUILayout.LabelField("Attenuation", GUILayout.Width(100));
		            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
		            EditorGUILayout.PropertyField(serializedObject.FindProperty("MinDistance"), GUIContent.none, GUILayout.Width(40));
		            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
		            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxDistance"), GUIContent.none, GUILayout.Width(40));
		            GUILayout.EndHorizontal();
		            
		            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("RollOffMode"), "Curve");
		            if (sc.RollOffMode == AudioRolloffMode.Custom)
			            EditorGUILayout.PropertyField(serializedObject.FindProperty("AttenuationCurve"), GUIContent.none);    
	            }
            }

            GUI.enabled = true;
            EditorGUILayout.Separator();
        }
    }
    
    [CustomEditor(typeof(SoundBlendContainer)), CanEditMultipleObjects]
	public class SoundBlendContainerInspector : SoundContainerInspector
	{
	}
	
	[CustomEditor(typeof(SoundRandomContainer)), CanEditMultipleObjects]
	public class SoundRandomContainerInspector : SoundContainerInspector
	{
		protected override void DrawPlayLogic()
		{
			EditorGUILayout.LabelField("Random Logic", EditorStyles.boldLabel);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AvoidRepeat"), "", 120);
                var randomOnLoop = serializedObject.FindProperty("RandomOnLoop");
				AsGuiDrawer.DrawProperty(randomOnLoop, "", 120);
                if (randomOnLoop.boolValue) 
                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("CrossFadeTime"), "", 120);
			}
			EditorGUILayout.Separator();
		}
	}
	
	[CustomEditor(typeof(SoundSwitchContainer)), CanEditMultipleObjects]
	public class SoundSwitchContainerInspector : SoundContainerInspector
	{
		protected override void DrawPlayLogic()
		{
			EditorGUILayout.LabelField("Switch Logic", EditorStyles.boldLabel);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SwitchImmediately"), "", 120);	                    
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("CrossFadeTime"), "", 120);
			}
			EditorGUILayout.Separator();
		}
		
		protected override void DrawChildEvents()
		{
			EditorGUILayout.LabelField("Child Sound Events", EditorStyles.boldLabel);    
			using (new EditorGUILayout.VerticalScope(GUI.skin.box))
			{
				AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AudioSwitchReference"), "Audio Switch");
				EditorGUILayout.LabelField("Switch Assignment");
				AsGuiDrawer.DrawList(serializedObject.FindProperty("SwitchEventMappings"));
			}
			EditorGUILayout.Separator();
		}
	}
	
	[CustomEditor(typeof(SoundSequenceContainer)), CanEditMultipleObjects]
	public class SoundSequenceContainerInspector : SoundContainerInspector
	{
	}
}