using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

public abstract class AudioObjectReferenceDrawer : PropertyDrawer 
{
    private int _pickerWindowId;
    private Rect _position;

    protected bool ShowButton(Rect position, SerializedProperty property, string objectType)
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
    
    private void GetSelectedObject(Rect position, SerializedProperty property)
    {        
        if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == _pickerWindowId && position == _position)
        {
            var obj = EditorGUIUtility.GetObjectPickerObject();
            property.stringValue = obj ? obj.name : "";
            _pickerWindowId = -1;
        }            
    }
}

[CustomPropertyDrawer(typeof(AudioEventReference))]
public class AudioEventReferenceDrawer : AudioObjectReferenceDrawer
{						
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {		
        EditorGUI.BeginProperty(position, label, property);						
        var totalWidth = position.width;
		
        position.width = 55;		
        var eventType = property.FindPropertyRelative("EventType");								
        EditorGUI.PropertyField(position, eventType, GUIContent.none);
		
        position.x += 57;
        position.width = totalWidth - 55;			
        if (ShowButton(position, property, "Event"))
        {
            var type = eventType.enumValueIndex;
            switch (type)
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
        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(SoundBankReference))]
public class SoundBankReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);				
        if (ShowButton(position, property, "Bank"))
            ShowPicker<SoundBank>(position);				
        EditorGUI.EndProperty();
    }	
}

[CustomPropertyDrawer(typeof(AudioParameterReference))]
public class AudioParameterReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);				
        if (ShowButton(position, property, "Parameter"))
            ShowPicker<AudioParameter>(position);
        EditorGUI.EndProperty();
    }	
}

[CustomPropertyDrawer(typeof(SetAudioParameterReference))]
public class SetAudioParameterReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {        
        EditorGUI.BeginProperty(position, label, property);        
        var totalWidth = position.width;
		
        position.width = 150;		        
        if (ShowButton(position, property, "Parameter"))
            ShowPicker<AudioParameter>(position);
        
        position.x += 152;
        position.width = totalWidth - 152;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("Value"), GUIContent.none);
        EditorGUI.EndProperty();
    }	
}

[CustomPropertyDrawer(typeof(AudioSwitchReference))]
public class AudioSwitchReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);				
        if (ShowButton(position, property, "Switch"))
            ShowPicker<AudioSwitch>(position);
        EditorGUI.EndProperty();
    }	
}

[CustomPropertyDrawer(typeof(SetSwitchReference))]
public class SetSwitchReferenceDrawer : AudioObjectReferenceDrawer
{							
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {        
        EditorGUI.BeginProperty(position, label, property);        
        var totalWidth = position.width;
		
        position.width = 130;		        
        if (ShowButton(position, property, "Switch"))
            ShowPicker<AudioSwitch>(position);
        
        position.x += 132;
        position.width = totalWidth - 132;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("Selection"), GUIContent.none);
        EditorGUI.EndProperty();
    }	
}
