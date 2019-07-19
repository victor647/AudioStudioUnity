using UnityEditor;
using UnityEngine;
using AudioStudio;

[CustomEditor(typeof(SpatialSetting)), CanEditMultipleObjects]
public class SpatialSettingInspector : Editor
{
    private SpatialSetting _spatialSetting;

    private void OnEnable()
    {
        _spatialSetting = target as SpatialSetting;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawAttenuation();
        DrawSpatial();
        AudioScriptGUI.DrawSaveButton(_spatialSetting);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAttenuation()
    {
        EditorGUILayout.LabelField("Attenuation", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Distance", GUILayout.Width(80));
            EditorGUILayout.LabelField("Min", GUILayout.Width(30));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MinDistance"), GUIContent.none,
                GUILayout.Width(50));
            EditorGUILayout.LabelField("Max", GUILayout.Width(30));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MaxDistance"), GUIContent.none,
                GUILayout.Width(50));
            GUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RollOffMode"));
        }
    }

    private void DrawSpatial()
    {
        EditorGUILayout.LabelField("3D Properties", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spatial Blend", GUILayout.Width(100));
            _spatialSetting.SpatialBlend = EditorGUILayout.Slider(_spatialSetting.SpatialBlend, 0f, 1f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spread Width", GUILayout.Width(100));
            _spatialSetting.Spread = EditorGUILayout.Slider(_spatialSetting.Spread, 0f, 360f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Doppler Level", GUILayout.Width(100));
            _spatialSetting.DopplerLevel = EditorGUILayout.Slider(_spatialSetting.DopplerLevel, 0f, 5f);
            GUILayout.EndHorizontal();
        }
    }
}