using UnityEngine;

namespace AudioStudio.Configs
{
	public abstract class AudioConfig : ScriptableObject
	{		
		public virtual void OnValidate()
		{
		}

		public abstract void CleanUp();
		public abstract bool IsValid();
	}
	
	public abstract class AudioController : AudioConfig
	{
		public abstract void Init();
		public abstract void Dispose();
		public override bool IsValid()
		{
			return true;
		}
	}
}
