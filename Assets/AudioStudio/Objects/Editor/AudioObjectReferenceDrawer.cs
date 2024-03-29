﻿using System.Linq;
using AudioStudio;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Editor;
using AudioStudio.Tools;
using UnityEditor;
using UnityEngine;

public abstract class AudioObjectReferenceDrawer : PropertyDrawer 
{
    private int _pickerWindowId;
    private Rect _position;

    protected virtual bool ShowButton(Rect position, SerializedProperty property, string objectType)
    {
        var name = property.FindPropertyRelative("Name");
        GetSelectedObject(position, name);
    
        var buttonStyle = new GUIStyle(EditorStyles.objectField) {alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal};        
        var buttonText = name.stringValue;		
        if (string.IsNullOrEmpty(buttonText))
        {
            buttonText = "No " + objectType + " Selected";
            buttonStyle.normal.textColor = Color.red;				
        }
        else 
            buttonStyle.normal.textColor = Color.white;
        return GUI.Button(position, buttonText, buttonStyle);
    }

    protected void ShowPicker<T>(Rect position) where T : ScriptableObject
    {
        _position = position;
        _pickerWindowId = GUIUtility.GetControlID(FocusType.Passive) + 100;        
        EditorGUIUtility.ShowObjectPicker<T>(null, false, "", _pickerWindowId);		
    }

    protected void GetSelectedObject(Rect position, SerializedProperty property)
    {        
        if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == _pickerWindowId && position == _position)
        {
            var obj = EditorGUIUtility.GetObjectPickerObject();
            property.stringValue = obj ? obj.name : "";
            _pickerWindowId = -1;
        }            
    }
}

[CustomPropertyDrawer(typeof(MusicTransitionReference))]
public class MusicTransitionReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShowButton(position, property, "Any"))
            ShowPicker<MusicContainer>(position);
    }

    protected override bool ShowButton(Rect position, SerializedProperty property, string emptyLabel)
    {
        var name = property.FindPropertyRelative("Name");
        GetSelectedObject(position, name);
    
        var buttonStyle = new GUIStyle(EditorStyles.objectField) {alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal};        
        var buttonText = name.stringValue;
        buttonStyle.normal.textColor = Color.white;
        if (string.IsNullOrEmpty(buttonText))
            buttonText = emptyLabel;
        return GUI.Button(position, buttonText, buttonStyle);
    }
}

[CustomPropertyDrawer(typeof(MusicSegmentReference))]
public class MusicSegmentReferenceDrawer : MusicTransitionReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShowButton(position, property, "N/A"))
            ShowPicker<MusicTrack>(position);
    }
}

[CustomPropertyDrawer(typeof(AudioEventReference))]
public class AudioEventReferenceDrawer : AudioObjectReferenceDrawer
{						
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var totalWidth = position.width;
		
        position.width = 55;		
        var eventType = property.FindPropertyRelative("Type");								
        EditorGUI.PropertyField(position, eventType, GUIContent.none);
        position.x += 57;
        
        position.width = totalWidth - 55;
        if (ShowButton(position, property, "Event"))
        {
            switch (eventType.enumValueIndex)
            {
                case 0:
                    ShowPicker<SoundContainer>(position);	
                    break;
                case 1:
                    ShowPicker<MusicContainer>(position);
                    break;
                case 2:
                    ShowPicker<VoiceEvent>(position);
                    break;
            }
        }
        DrawAuditionButtons(property);
    }

    protected void DrawAuditionButtons(SerializedProperty property)
    {
        if (Application.isPlaying)
        {
            var eventType = property.FindPropertyRelative("Type").enumValueIndex;			
            GUI.contentColor = Color.green;
            if (GUILayout.Button("▶", EditorStyles.miniButtonLeft, GUILayout.Width(20f)))
            {
                var eventName = property.FindPropertyRelative("Name").stringValue;
                var component = property.serializedObject.targetObject as MonoBehaviour;
                var gameObject = component ? component.gameObject : GlobalAudioEmitter.GameObject;
                switch (eventType)
                {
                    case 0:
                        AudioManager.PlaySound(eventName, gameObject, 0, null, AudioTriggerSource.InspectorAudition);
                        break;
                    case 1:
                        AudioManager.PlayMusic(eventName, 0, gameObject, AudioTriggerSource.InspectorAudition);
                        break;
                    case 2:
                        AudioManager.PlayVoice(eventName, gameObject, 0, null, AudioTriggerSource.InspectorAudition);
                        break;
                }
            }
            GUI.contentColor = Color.red;
            if (GUILayout.Button("■", EditorStyles.miniButtonRight, GUILayout.Width(20f)))
            {
                var eventName = property.FindPropertyRelative("Name").stringValue;
                var component = property.serializedObject.targetObject as MonoBehaviour;
                var gameObject = component ? component.gameObject : GlobalAudioEmitter.GameObject;
                switch (eventType)
                {
                    case 0:
                        AudioManager.StopSound(eventName, gameObject, 0, AudioTriggerSource.InspectorAudition);
                        break;
                    case 1:
                        AudioManager.StopMusic(0, gameObject, AudioTriggerSource.InspectorAudition);
                        break;
                    case 2:
                        AudioManager.StopVoice(eventName, gameObject, 0, AudioTriggerSource.InspectorAudition);
                        break;
                }
            }
            GUI.contentColor = Color.white;
        }
    }
}

[CustomPropertyDrawer(typeof(PostEventReference))]
public class PostEventReferenceDrawer : AudioEventReferenceDrawer
{						
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        base.OnGUI(position, property, label);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        AsGuiDrawer.DrawProperty(property.FindPropertyRelative("Action"), "", 40);
        AsGuiDrawer.DrawProperty(property.FindPropertyRelative("FadeTime"), "", 70, 40);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(" ");
    }
}

[CustomPropertyDrawer(typeof(SoundBankReference))]
public class SoundBankReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShowButton(position, property, "Bank"))
            ShowPicker<SoundBank>(position);

        if (Application.isPlaying)
        {
            var bankName = property.FindPropertyRelative("Name").stringValue;
            GUI.contentColor = Color.green;
            if (GUILayout.Button("▶", EditorStyles.miniButtonLeft, GUILayout.Width(20f)))
                AudioManager.LoadBank(bankName, null, null, AudioTriggerSource.InspectorAudition);
            GUI.contentColor = Color.red;
            if (GUILayout.Button("■", EditorStyles.miniButtonRight, GUILayout.Width(20f)))
                AudioManager.UnloadBank(bankName, null, AudioTriggerSource.InspectorAudition);
            GUI.contentColor = Color.white;
        }
    }	
}

[CustomPropertyDrawer(typeof(LoadBankReference))]
public class LoadBankReferenceDrawer : SoundBankReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        base.OnGUI(position, property, label);
        GUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(property.FindPropertyRelative("UnloadOnDisable"));
        var eventsProperty = property.FindPropertyRelative("LoadFinishEvents");
        AsGuiDrawer.DrawList(eventsProperty, "Play On Load Finish:", objects =>
        {
            var events = objects.Select(obj => obj as AudioEvent).Where(a => a).ToArray();
            foreach (var evt in events)
            {
                eventsProperty.arraySize++;
                var item = eventsProperty.GetArrayElementAtIndex(eventsProperty.arraySize - 1);
                item.FindPropertyRelative("Name").stringValue = evt.name;
                switch (evt)
                {
                    case SoundContainer _:
                        item.FindPropertyRelative("Type").enumValueIndex = 0;
                        break;
                    case MusicContainer _:
                        item.FindPropertyRelative("Type").enumValueIndex = 1;
                        break;
                    case VoiceEvent _:
                        item.FindPropertyRelative("Type").enumValueIndex = 2;
                        break;
                }
            }
        });
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(" ");
    }
}

[CustomPropertyDrawer(typeof(AudioParameterReference))]
public class AudioParameterReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShowButton(position, property, "Parameter"))
            ShowPicker<AudioParameter>(position);
    }	
}

[CustomPropertyDrawer(typeof(SetAudioParameterReference))]
public class SetAudioParameterReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var totalWidth = position.width;
		
        position.width = 150;		        
        if (ShowButton(position, property, "Parameter"))
            ShowPicker<AudioParameter>(position);
        
        position.x += 152;
        position.width = totalWidth - 152;
        var value = property.FindPropertyRelative("Value");
        EditorGUI.PropertyField(position, value, GUIContent.none);

        if (Application.isPlaying)
        {
            GUI.contentColor = Color.green;
            if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(20f)))
            {
                var parameterName = property.FindPropertyRelative("Name").stringValue;
                var component = property.serializedObject.targetObject as MonoBehaviour;
                var gameObject = component ? component.gameObject : GlobalAudioEmitter.GameObject;
                AudioManager.SetParameterValue(parameterName, value.floatValue, gameObject, AudioTriggerSource.InspectorAudition);
            }
            GUI.contentColor = Color.white;
        }
    }	
}

[CustomPropertyDrawer(typeof(AudioSwitchReference))]
public class AudioSwitchReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShowButton(position, property, "Switch"))
            ShowPicker<AudioSwitch>(position);
    }	
}

[CustomPropertyDrawer(typeof(SetSwitchReference))]
public class SetSwitchReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var totalWidth = position.width;
		
        position.width = 120;		        
        if (ShowButton(position, property, "Switch"))
            ShowPicker<AudioSwitch>(position);
        
        position.x += 122;
        position.width = totalWidth - 122;
        var selection = property.FindPropertyRelative("Selection");
        EditorGUI.PropertyField(position, selection, GUIContent.none);

        if (Application.isPlaying)
        {
            GUI.contentColor = Color.green;
            if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(20f)))
            {
                var parameterName = property.FindPropertyRelative("Name").stringValue;
                var component = property.serializedObject.targetObject as MonoBehaviour;
                var gameObject = component ? component.gameObject : GlobalAudioEmitter.GameObject;
                AudioManager.SetSwitch(parameterName, selection.stringValue, gameObject, AudioTriggerSource.InspectorAudition);
            }
            GUI.contentColor = Color.white;
        }
    }	
}