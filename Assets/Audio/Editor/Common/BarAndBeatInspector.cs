using UnityEditor;
using UnityEngine;

namespace AudioStudio
{	
	[CustomPropertyDrawer(typeof(BarAndBeat))]
	public class BarAndBeatInspector : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var oldIndentLevel = EditorGUI.indentLevel;
			label = EditorGUI.BeginProperty(position, label, property);
			var contentPosition = EditorGUI.PrefixLabel(position, label);

			EditorGUI.indentLevel = 0;

			var contentWidth = contentPosition.width;
			
			contentPosition.height = 16;
			EditorGUIUtility.labelWidth = 40;

			contentPosition.width = contentWidth * 0.25f;			
			EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("Bar"), GUIContent.none);
			contentPosition.x += contentPosition.width;
			EditorGUI.LabelField(contentPosition, "bars");
		
			contentPosition.x += contentPosition.width;					
			EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("Beat"), GUIContent.none);
			contentPosition.x += contentPosition.width;
			EditorGUI.LabelField(contentPosition, "beats");

			EditorGUI.EndProperty();
			EditorGUI.indentLevel = oldIndentLevel;  
		}
	}
	
}
