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
            AudioScriptGUI.DrawSaveButton(_soundContainer);
            serializedObject.ApplyModifiedProperties();
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
	                    EditorGUILayout.PropertyField(serializedObject.FindProperty("AvoidRepeat"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomOnLoop"));
                        if (_soundContainer.RandomOnLoop) DrawProperty("CrossFadeTime", "Cross Fade Time", 116, 50);
                        EditorGUILayout.LabelField("Sound Containers/Clips");
                        AudioScriptGUI.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);		                    
                        break;
	                case SoundPlayLogic.Layer:
                    case SoundPlayLogic.SequenceStep:                    
                        EditorGUILayout.LabelField("Sound Containers/Clips");
                        AudioScriptGUI.DrawList(serializedObject.FindProperty("ChildEvents"), "", AddChildEvent);					
                        break;
                    case SoundPlayLogic.Switch:	                    	                    	                                            
                        DrawProperty("SwitchImmediately", "Switch Immediately");	                    
	                    if (_soundContainer.SwitchImmediately) DrawProperty("CrossFadeTime", "Cross Fade Time", 116, 50);		                    	                    
                        DrawProperty("AudioSwitchReference", "Switch", 100);			
                        EditorGUILayout.LabelField("Switch Assignment");
                        AudioScriptGUI.DrawList(serializedObject.FindProperty("SwitchEventMappings"), "", AddChildEvent);
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
                DrawProperty("DefaultFadeInTime", "Fade In", 116, 50);
                DrawProperty("DefaultFadeOutTime", "Out", 30, 40);
                GUILayout.EndHorizontal();
                
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
                    EditorGUILayout.LabelField("Volume", GUILayout.Width(116));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Volume"), GUIContent.none,GUILayout.Width(50));                
                    EditorGUILayout.LabelField("Random", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizeVolume"), GUIContent.none, GUILayout.Width(15));
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
                    EditorGUILayout.LabelField("Pitch", GUILayout.Width(116));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Pitch"), GUIContent.none,GUILayout.Width(50));                
                    EditorGUILayout.LabelField("Random", GUILayout.Width(50));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomizePitch"), GUIContent.none, GUILayout.Width(15));
                }
                GUILayout.EndHorizontal();                                                
                DrawProperty("Pan");                                                
                DrawFilters(sc);   
                DrawSubMixer(sc);
                DrawParameterMappings();
                GUI.enabled = true;
            }
            EditorGUILayout.Separator();            
        }

        protected void DrawVoiceManagement(SoundContainer sc)
        {   
            if (sc.IndependentEvent) DrawProperty("EnableVoiceLimit", "Limit Voices");
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
                DrawProperty("VoiceLimiter", "Voice Limiter");
                if (sc.VoiceLimiter) 
                    DrawProperty("Priority", "Priority", 100, 50);
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
            EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel); 
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.BeginHorizontal();                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IndependentEvent"));
                if (!sc.IndependentEvent)
                {
                    if (GUILayout.Button("Select Parent")) Selection.objects = new Object[] {sc.ParentContainer};	
                }	
                EditorGUILayout.EndHorizontal();                                         
            }
            EditorGUILayout.Separator();
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("IsUpdatePosition"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("EmitterGameObject"));
                if (sc.IsUpdatePosition) EditorGUILayout.PropertyField(serializedObject.FindProperty("SpatialSetting"));
                GUI.enabled = true;
            }
            EditorGUILayout.Separator();
        }
    }
}