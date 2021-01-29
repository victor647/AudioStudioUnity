using System.Globalization;
using System.Reflection;
using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;

namespace AudioStudio.Editor
{
    public class MusicMarkerPreview : EditorWindow
    {
	    private MusicTrack _musicTrack;
	    private float[] _minMaxData;
	    private Texture2D _entryCueTexture, _exitCueTexture;

	    public void Init(MusicTrack track)
	    {
		    _musicTrack = track;
		    var iconFolder = "Assets/" + AudioPathSettings.AudioStudioLibraryPath + "/Objects/Editor/Icons/";
		    _entryCueTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconFolder + "MusicTrackEntryCue.png");
		    _exitCueTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconFolder + "MusicTrackExitCue.png");
	    }

	    public void OnGUI()
	    {
		    GUILayout.Space(32);
		    GUILayout.Box("", (GUIStyle)"PreBackground", GUILayout.Width(position.width), GUILayout.Height(position.height - 40));
		    DrawWaveForm();
		    DrawMarkers();
	    }

	    private void DrawMarkers()
	    {
		    var barPositionInSeconds = _musicTrack.PickupBeats * _musicTrack.Markers[0].BeatDurationInSeconds();
		    var markerIndex = 0;
		    var currentBarNumber = 0;
		    var lastGridPositionX = -999f;
		    var positionX = Mathf.RoundToInt(position.width * barPositionInSeconds / _musicTrack.Clip.length);
		    DrawMarker(_musicTrack.Markers[0], positionX);
		    // entry cue flag
		    GUI.DrawTexture(new Rect(positionX, 32, 16, 16), _entryCueTexture);
		    GUI.DrawTexture(new Rect(positionX, 32, 1, position.height - 40), EditorGUIUtility.whiteTexture);
		    
		    while (barPositionInSeconds < _musicTrack.Clip.length)
		    {
			    positionX = Mathf.RoundToInt(position.width * barPositionInSeconds / _musicTrack.Clip.length);
			    // more markers awaiting
			    if (markerIndex < _musicTrack.Markers.Length - 1)
			    {
				    // go to next marker
				    if (currentBarNumber >= _musicTrack.Markers[markerIndex + 1].BarNumber)
				    {
					    markerIndex++;
					    DrawMarker(_musicTrack.Markers[markerIndex], positionX);
				    }
			    }
			    var currentMarker = _musicTrack.Markers[markerIndex];
			    DrawTimelineGrid(currentBarNumber, positionX, ref lastGridPositionX);
			    // increment a bar's duration
			    barPositionInSeconds += currentMarker.BarDurationInSeconds;
			    currentBarNumber++;
			    
			    // exit cue flag
			    if (currentBarNumber == _musicTrack.ExitPosition.Bar)
			    {
				    var exitPositionInSeconds = barPositionInSeconds + _musicTrack.ExitPosition.Beat * currentMarker.BeatDurationInSeconds();
				    positionX = Mathf.RoundToInt(position.width * exitPositionInSeconds / _musicTrack.Clip.length);
				    GUI.DrawTexture(new Rect(positionX - 16, 32, 16, 16), _exitCueTexture);
				    GUI.DrawTexture(new Rect(positionX, 32, 1, position.height - 40), EditorGUIUtility.whiteTexture);
			    }
		    }
	    }

	    private void DrawMarker(MusicMarker marker, float positionX)
	    {
		    var text = $"{marker.KeyCenter} {marker.Tempo} {marker.BeatsPerBar}/{marker.BeatDuration.ToString().Substring(1)}";
		    GUI.Label(new Rect(positionX, 0, 80, 16), text);
	    }

	    private void DrawTimelineGrid(int barNumber, float positionX, ref float lastGridPositionX)
	    {
		    if (positionX - lastGridPositionX >= 40)
		    {
			    GUI.DrawTexture(new Rect(positionX, 16, 1, 16), EditorGUIUtility.whiteTexture);
			    GUI.Label(new Rect(positionX + 1, 16, 30, 16), barNumber.ToString(CultureInfo.CurrentCulture));
			    lastGridPositionX = positionX;
		    }
	    }

	    private void DrawWaveForm (float start = 0, float length = 0)
	    {
		    if (_minMaxData != null)
		    {
			    if (length == 0) // draw full length
				    length = _musicTrack.Clip.length;
                var curveColor = new Color(255 / 255f, 168 / 255f, 7 / 255f);
                var numChannels = _musicTrack.Clip.channels;
                var numSamples = Mathf.FloorToInt(_minMaxData.Length / (2f * numChannels) * (length / _musicTrack.Clip.length));

                AudioCurveRendering.DrawMinMaxFilledCurve(
	                GUILayoutUtility.GetLastRect(),
                    delegate (float x, out Color col, out float minValue, out float maxValue) {
                        col = curveColor;
                        var p = Mathf.Clamp(x * (numSamples - 2), 0.0f, numSamples - 2);
                        var i = (int)Mathf.Floor(p);
                        var s = (start / _musicTrack.Clip.length) * Mathf.FloorToInt(_minMaxData.Length / (2 * numChannels) - 2);
                        var si = (int)Mathf.Floor(s);

                        var offset1 = Mathf.Clamp(((i + si) * numChannels) * 2, 0, _minMaxData.Length - 2);
                        var offset2 = Mathf.Clamp(offset1 + numChannels * 2, 0, _minMaxData.Length - 2);

                        minValue = Mathf.Min(_minMaxData[offset1 + 1], _minMaxData[offset2 + 1]);
                        maxValue = Mathf.Max(_minMaxData[offset1 + 0], _minMaxData[offset2 + 0]);
                        if (minValue > maxValue)
                        {
	                        var tmp = minValue;
	                        minValue = maxValue; 
	                        maxValue = tmp;
                        }
                    }
                );
            }
            else
            {
	            // If execution has reached this point, the waveform data needs generating
	            var path = AssetDatabase.GetAssetPath(_musicTrack.Clip);
	            if (path == null)
		            return;
	            var importer = AssetImporter.GetAtPath(path);
	            if (importer == null)
		            return;
	            var assembly = Assembly.GetAssembly(typeof(AssetImporter));
	            if (assembly == null)
		            return;
	            var type = assembly.GetType("UnityEditor.AudioUtil");
	            if (type == null)
		            return;
	            var audioUtilGetMinMaxData = type.GetMethod("GetMinMaxData");
	            if (audioUtilGetMinMaxData == null)
		            return;
	            _minMaxData = audioUtilGetMinMaxData.Invoke(null, new object[] {importer}) as float[];
            }
	    }
    }
}