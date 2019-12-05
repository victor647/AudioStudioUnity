using System;
using AudioStudio.Configs;
using AudioStudio.Tools;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CreateAssetMenu(fileName = "New SoundCaster", menuName = "Audio/Sound Caster")]
    public class SoundCaster : ScriptableObject
    {
        public AudioEventReference[] AudioEvents = new AudioEventReference[0];
        public SetSwitchReference[] AudioSwitches = new SetSwitchReference[0];
        public SetAudioParameterReference[] AudioParameters = new SetAudioParameterReference[0];
        public SoundBankReference[] SoundBanks = new SoundBankReference[0];
    }

    [CustomEditor(typeof(SoundCaster))]
    public class SoundCasterInspector : UnityEditor.Editor
    {
        private SoundCaster _component;

        private void OnEnable()
        {
            _component = target as SoundCaster;
        }

        public override void OnInspectorGUI()
        {
            if (Application.isPlaying && !AudioInitSettings.Initialized)
                AudioInitSettings.Instance.InitializeWithoutLoading();

            serializedObject.Update();
            DrawList(serializedObject.FindProperty("SoundBanks"), "Sound Banks:", LoadBank, UnloadBank);
            DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events:", PlayEvent, StopEvent);
            DrawList(serializedObject.FindProperty("AudioSwitches"), "Audio Switches:", SetSwitch);
            DrawList(serializedObject.FindProperty("AudioParameters"), "Audio Parameter:", SetParameterValue);
            AsGuiDrawer.DrawSaveButton(_component);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawList(SerializedProperty list, string labelName, Action<int> playEvent, Action<int> stopEvent = null)
        {
            EditorGUILayout.LabelField(labelName, EditorStyles.boldLabel);
            var dropArea = new EditorGUILayout.VerticalScope(GUI.skin.box);
            using (dropArea)
            {
                if (list.arraySize == 0)
                {
                    GUI.contentColor = Color.yellow;
                    if (GUILayout.Button("Create", EditorStyles.miniButton))
                        list.arraySize++;
                    GUI.contentColor = Color.white;
                }
                else
                {
                    for (var i = 0; i < list.arraySize; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);

                        if (Application.isPlaying)
                        {
                            GUI.contentColor = Color.green;
                            if (GUILayout.Button("▶", stopEvent != null ? EditorStyles.miniButtonLeft : EditorStyles.miniButton, GUILayout.Width(20f)))
                                playEvent.Invoke(i);

                            if (stopEvent != null)
                            {
                                GUI.contentColor = Color.red;
                                if (GUILayout.Button("■", EditorStyles.miniButtonRight, GUILayout.Width(20f)))
                                    stopEvent.Invoke(i);
                            }

                            GUI.contentColor = Color.white;
                        }

                        AsGuiDrawer.DrawAddDeleteButtons(list, i);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private void PlayEvent(int index)
        {
            _component.AudioEvents[index].Post(null, -1f, AudioTriggerSource.InspectorAudition);
        }

        private void StopEvent(int index)
        {
            _component.AudioEvents[index].Stop(null, -1f, AudioTriggerSource.InspectorAudition);
        }

        private void LoadBank(int index)
        {
            _component.SoundBanks[index].Load(AudioTriggerSource.InspectorAudition);
        }

        private void UnloadBank(int index)
        {
            _component.SoundBanks[index].Unload(AudioTriggerSource.InspectorAudition);
        }

        private void SetSwitch(int index)
        {
            _component.AudioSwitches[index].SetValue(null, AudioTriggerSource.InspectorAudition);
        }

        private void SetParameterValue(int index)
        {
            _component.AudioParameters[index].SetValue(AudioTriggerSource.InspectorAudition);
        }
    }
}