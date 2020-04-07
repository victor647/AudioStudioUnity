using System;
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
			if (!IsValid())
				AsUnityHelper.DebugToProfiler(Severity.Warning, AudioObjectType.Component, AudioAction.Activate, AudioTriggerSource.Initialization, GetType().Name, gameObject, "Component is empty");
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

