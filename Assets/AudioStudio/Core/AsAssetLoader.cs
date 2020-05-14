using System;
using System.Collections.Generic;
using AudioStudio.Configs;

namespace AudioStudio
{
	internal static partial class AsAssetLoader
	{
		private static readonly Dictionary<string, AudioEvent> _audioEvents = new Dictionary<string, AudioEvent>();
		private static readonly Dictionary<string, MusicInstrument> _musicInstruments = new Dictionary<string, MusicInstrument>();
		private static readonly Dictionary<string, AudioParameter> _audioParameters = new Dictionary<string, AudioParameter>();
		private static readonly Dictionary<string, AudioSwitch> _audioSwitches = new Dictionary<string, AudioSwitch>();

		private static string ShortPath(string longPath)
		{
			longPath = longPath.Replace("\\", "/");
			var index = longPath.IndexOf("Audio", StringComparison.Ordinal);
			return index >= 0 ? longPath.Substring(index) : longPath;
		}

		internal static AudioEvent GetAudioEvent(string eventName)
		{
			return _audioEvents.ContainsKey(eventName) ? _audioEvents[eventName] : null;
		}	
		
		internal static AudioParameter GetAudioParameter(string parameterName)
		{
			return _audioParameters.ContainsKey(parameterName) ? _audioParameters[parameterName] : null;
		}	
		
		internal static AudioSwitch GetAudioSwitch(string switchName)
		{
			return _audioSwitches.ContainsKey(switchName) ? _audioSwitches[switchName] : null;
		}
	}
}