using System;

namespace AudioStudio
{
	public enum AnimationAudioState
	{
		None,
		Show,
		Battle
	}

	[Flags]
	public enum AudioTags
	{		
		None = 0,
		AllTags = ~0,
		Camera = 0x1,
		Player = 0x2,
		Enemy = 0x4,
		Ground = 0x8,
		Water = 0x10
	}
	
	public enum Languages
	{
		Chinese,
		English	
	}
}


