using UnityEditor;
using UnityEngine;
using AudioStudio.Components;

namespace AudioStudio.Editor
{
    [CustomEditor(typeof(MusicTransport))]
    public class MusicTransportInspector : UnityEditor.Editor
    {
        private MusicTransport _musicTransport;

        private void OnEnable()
        {
            _musicTransport = target as MusicTransport;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Playback Status", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Playing Status");
                EditorGUILayout.LabelField(_musicTransport.PlayingStatus.ToString());
                GUILayout.EndHorizontal();

                if (_musicTransport.PlayingStatus == PlayingStatus.Idle || _musicTransport.PlayingStatus == PlayingStatus.Stopping) return;

                if (_musicTransport.TransitioningStatus != TransitioningStatus.None)
                {
                    EditorGUILayout.LabelField("Transitioning");
                    EditorGUILayout.LabelField("  Current Sample: " + _musicTransport.CurrentSample);
                    EditorGUILayout.LabelField("  Exit Sample: " + _musicTransport.TransitionExitSample);
                    EditorGUILayout.LabelField("  Enter Sample: " + _musicTransport.TransitionEnterSample);
                }
                
                if (_musicTransport.SequencingStatus != SequencingStatus.None)
                {
                    EditorGUILayout.LabelField("Sequencing");
                    EditorGUILayout.LabelField("  Current Sample: " + _musicTransport.CurrentSample);
                    EditorGUILayout.LabelField("  Exit Sample: " + _musicTransport.SequenceExitSample);
                    EditorGUILayout.LabelField("  Enter Sample: " + _musicTransport.SequenceEnterSample);
                }
                
                
                if (_musicTransport.SwitchingStatus != SwitchingStatus.None)
                {
                    EditorGUILayout.LabelField("Switching");
                    EditorGUILayout.LabelField("  Current Sample: " + _musicTransport.CurrentSample);
                    EditorGUILayout.LabelField("  Enter Sample: " + _musicTransport.SwitchEnterSample);
                }
            }

            if (_musicTransport.TransitioningStatus != TransitioningStatus.Transitioning && _musicTransport.SequencingStatus != SequencingStatus.ChangingSequence)
            {
                EditorGUILayout.LabelField("Music Sync", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Key Center");
                    EditorGUILayout.LabelField(_musicTransport.CurrentKey.ToString());
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Tempo");
                    var tempo = (_musicTransport.CurrentMarker.Tempo * _musicTransport.PlayHeadAudioSource.pitch).ToString("0.00") + "    " +
                                _musicTransport.CurrentMarker.BeatsPerBar + "/" + _musicTransport.CurrentMarker.BeatDuration.ToString().Substring(1);
                    EditorGUILayout.LabelField(tempo);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Play Head Position");
                    var playHead = (_musicTransport.PlayHeadPosition.Bar + 1) + "." + (_musicTransport.PlayHeadPosition.Beat + 1);
                    EditorGUILayout.LabelField(playHead);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Loop Position");
                    var loopPosition = (_musicTransport.ExitPosition.Bar + 1) + "." + (_musicTransport.ExitPosition.Beat + 1);
                    EditorGUILayout.LabelField(loopPosition);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Remaining Loops");
                    EditorGUILayout.LabelField(_musicTransport.ActiveMusicData.RemainingLoops == 0 ? "Infinite" : _musicTransport.ActiveMusicData.RemainingLoops.ToString());
                    GUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.LabelField("Track Queue", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Active Music Event: ");
                var activeContainer = _musicTransport.CurrentEvent;
                var eventName = activeContainer ? activeContainer.name : "N/A";
                EditorGUILayout.LabelField(eventName);
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("  Track(s) Playing: ");
                foreach (var track in _musicTransport.ActiveTracks)
                {
                    EditorGUILayout.LabelField("    " + track.name);
                }

                EditorGUILayout.Separator();

                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Next Music Event: ");
                var nextContainer = _musicTransport.QueuedEvent;
                eventName = nextContainer ? nextContainer.name : "N/A";
                EditorGUILayout.LabelField(eventName);
                GUILayout.EndHorizontal();

                EditorGUILayout.LabelField("  Track(s) in Queue: ");
                foreach (var track in _musicTransport.QueuedTracks)
                {
                    EditorGUILayout.LabelField("    " + track.name);
                }
            }

            GUI.contentColor = Color.red;
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                _musicTransport.Stop();
        }
    }
}