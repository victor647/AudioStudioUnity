using System.Collections.Generic;
using AudioStudio.Configs;
using AudioStudio.Tools;
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
		
		protected void PostEvents(IEnumerable<PostEventReference> events, AudioTriggerSource trigger, GameObject emitter = null)
		{
			foreach (var evt in events)
			{				
				evt.Post(emitter, trigger);
			}  
		}
	}
}

