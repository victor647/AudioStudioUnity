using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
    [CreateAssetMenu(fileName = "New SoundCaster", menuName = "AudioStudio/Sound Caster")]
    public class SoundCaster : ScriptableObject
    {
        public PostEventReference[] AudioEvents = new PostEventReference[0];
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
                AudioInitSettings.Instance.Initialize(false);

            serializedObject.Update();
            AsGuiDrawer.DrawList(serializedObject.FindProperty("SoundBanks"), "Sound Banks:");
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioEvents"), "Audio Events:");
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioSwitches"), "Audio Switches:");
            AsGuiDrawer.DrawList(serializedObject.FindProperty("AudioParameters"), "Audio Parameter:");
            AsGuiDrawer.DrawSaveButton(_component);
            serializedObject.ApplyModifiedProperties();
        }
    }
}