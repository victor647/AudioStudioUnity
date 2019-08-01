using AudioStudio.Configs;
using UnityEngine;

namespace AudioStudio.Components
{
	public abstract class AsComponent : MonoBehaviour
	{
		private void Awake()
		{
			if (!IsValid()) Destroy(this);
		}
		
		public virtual bool IsValid()
		{
			return true;
		}			
		
		protected static void PostEvents(AudioEventReference[] events, GameObject go = null)
		{
			foreach (var evt in events)
			{				
				evt.Post(go);
			}  
		}
	}
}