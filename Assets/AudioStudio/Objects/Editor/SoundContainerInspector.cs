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
            DrawChildEvents();		
            DrawAudioControls(_soundContainer);
            DrawVoiceManagement(_soundContainer);
            serializedObject.ApplyModifiedProperties();
            DrawAuditionButtons(_soundContainer);
            AsGuiDrawer.DrawSaveButton(_soundContainer);
        }       	                
        
        private void DrawChildEvents()
		{			
			EditorGUILayout.LabelField("Child Events", EditorStyles.boldLabel);    
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {			
	            EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayLogic"));
                switch (_soundContainer.PlayLogic)
                {                                                                    
                    case SoundPlayLogic.Random:
                        AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AvoidRepeat"), "  Avoid Repeat", 120);     
                        AsGuiDrawer.DrawProperty(serializedObject.FindProperty("RandomOnLoop"), "  Random On Loop", 120);
                        if (_soundContainer.RandomOnLoop) 
                            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("CrossFadeTime"), "  Cross Fade Time", 120);
                        EditorGUILayout.LabelField("Sound Containers/Clips");
                        AsGuiDrawer.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);		                    
                        break;
	                case SoundPlayLogic.Layer:
                    case SoundPlayLogic.SequenceStep:                    
                        EditorGUILayout.LabelField("Sound Containers/Clips");
                        AsGuiDrawer.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);					
                        break;
                    case SoundPlayLogic.Switch:	                    	                    	                                            
                        AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SwitchImmediately"), "  Switch Immediately", 120);	                    
	                    if (_soundContainer.SwitchImmediately) 
                            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("CrossFadeTime"), "   Fade Time", 120);		                    	                    
                        AsGuiDrawer.DrawProperty(serializedObject.FindProperty("AudioSwitchReference"), "Audio Switch");			
                        EditorGUILayout.LabelField("Switch Assignment");
                        AsGuiDrawer.DrawList(serializedObject.FindProperty("SwitchEventMappings"), "", AddChildEvent);
                        break;
                }					
            }
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
            if (sc.IndependentEvent) AsGuiDrawer.DrawProperty(serializedObject.FindProperty("EnableVoiceLimit"), "Limit Voices");
            if (!sc.EnableVoiceLimit) return;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Voice Management", EditorStyles.boldLabel, GUILayout.Width(150));            
            if (!sc.IndependentEvent)
            {                    
                EditorGUILayout.LabelField("Override", GUILayout.Width(60));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideVoicing"), GUIContent.none);
                if (!sc.OverrideVoicing) GUI.enabled = false;
            }            
            GUILayout.EndHorizontal();            
            
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
                GUI.enabled = true;
            }  
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

        private static void DrawCascade(SoundContainer sc, int indentLevel = 0)
        {
            if (!sc) return;
            var text = new string(' ', indentLevel * 3);
            text += sc is SoundClip ? sc.name : sc.name + " (" + sc.PlayLogic + ")";
            
            if (GUILayout.Button(text, Selection.activeObject == sc ? EditorStyles.whiteLabel : EditorStyles.label))
                Selection.activeObject = sc;
            if (sc.PlayLogic == SoundPlayLogic.Switch)
            {
                foreach (var eventMapping in sc.SwitchEventMappings)
                {
                    DrawCascade((SoundContainer)eventMapping.AudioEvent, indentLevel + 1);
                }
            }
            else
            {
                foreach (var childEvent in sc.ChildEvents)
                {
                    DrawCascade(childEvent, indentLevel + 1);
                }
            }
        }
        
        protected void Draw3DSetting(SoundContainer sc)
        {			
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("3D Settings", EditorStyles.boldLabel, GUILayout.Width(150)); 
            if (!sc.IndependentEvent)
            {                    
                EditorGUILayout.LabelField("Override", GUILayout.Width(60));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("OverrideSpatial"), GUIContent.none);
                if (!sc.OverrideSpatial) GUI.enabled = false;
            }
            GUILayout.EndHorizontal();
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                AsGuiDrawer.DrawProperty(serializedObject.FindProperty("Is3D"), "3D Positioning");
                if (sc.Is3D)
                {
                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SpatialBlend"));
                    AsGuiDrawer.DrawProperty(serializedObject.FindProperty("SpreadWidth"));
                    DrawAttenuation(sc);
                }
                GUI.enabled = true;
            }
            EditorGUILayout.Separator();
        }
        
        private void DrawAttenuation(SoundContainer sc)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Attenuation", GUILayout.Width(100));
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MinDistance"), GUIContent.none,
                GUILayout.MinWidth(40));
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxDistance"), GUIContent.none,
                GUILayout.MinWidth(40));
            GUILayout.EndHorizontal();
            AsGuiDrawer.DrawProperty(serializedObject.FindProperty("RollOffMode"), "Curve");
            if (sc.RollOffMode == AudioRolloffMode.Custom)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("AttenuationCurve"), GUIContent.none);    
        }
    }
}