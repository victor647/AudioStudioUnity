using AudioStudio;
using UnityEditor;

[CustomEditor(typeof(VoiceLimiter)), CanEditMultipleObjects]
public class VoiceLimiterInspector : Editor
{
		private VoiceLimiter _limiter;

		private void OnEnable()
		{
			_limiter = target as VoiceLimiter;
		}
	
    public override void OnInspectorGUI()
    {
        serializedObject.Update();																
        EditorGUILayout.PropertyField(serializedObject.FindProperty("VoiceRemovalRule"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxVoicesLimit"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("FadeOutTime"));
        
        AudioScriptGUI.DrawSaveButton(_limiter);
        serializedObject.ApplyModifiedProperties();
    }
}