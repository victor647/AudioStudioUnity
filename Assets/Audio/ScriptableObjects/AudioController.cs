using UnityEngine;

namespace AudioStudio
{
	public abstract class AudioObject : ScriptableObject
	{		
		public virtual void OnValidate()
		{
		}

		public abstract void CleanUp();
	}
	
	public abstract class AudioController : AudioObject
	{
		public abstract void Init();
		public abstract void Dispose();
	}
}

