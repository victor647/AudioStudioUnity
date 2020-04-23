using AudioStudio.Components;
using UnityEditor;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(GlobalAudioEmitter))]
    public class GlobalAudioEmitterInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            AudioManager.SoundEnabled = EditorGUILayout.Toggle("Sound Enabled", AudioManager.SoundEnabled);
            AudioManager.SoundVolume = EditorGUILayout.Slider("Sound Volume", AudioManager.SoundVolume, 0 , 100);
            AudioManager.MusicEnabled = EditorGUILayout.Toggle("Music Enabled", AudioManager.MusicEnabled);
            AudioManager.MusicVolume = EditorGUILayout.Slider("Music Volume", AudioManager.MusicVolume, 0 , 100);
            AudioManager.VoiceEnabled = EditorGUILayout.Toggle("Voice Enabled", AudioManager.VoiceEnabled);
            AudioManager.VoiceVolume = EditorGUILayout.Slider("Voice Volume", AudioManager.VoiceVolume, 0 , 100);
            AudioManager.VoiceLanguage = (Languages) EditorGUILayout.EnumPopup("Voice Language", AudioManager.VoiceLanguage);
        }
    }
}