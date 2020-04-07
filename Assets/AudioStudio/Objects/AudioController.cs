using System;
using UnityEngine;

namespace AudioStudio.Configs
{
	public abstract class AudioConfig : ScriptableObject, IComparable
	{		
		public virtual void OnValidate()
		{
		}

		public abstract void CleanUp();
		public abstract bool IsValid();
		public int CompareTo(object obj)
		{
			var other = obj as AudioConfig;
			return other ? String.Compare(name, other.name, StringComparison.OrdinalIgnoreCase) : 0;
		}
	}
	
	public abstract class AudioController : AudioConfig
	{
		internal abstract void Init();
		internal abstract void Dispose();
		public override bool IsValid()
		{
			return true;
		}
	}

	public abstract class AudioControllerInstance : MonoBehaviour
	{
	}
}

