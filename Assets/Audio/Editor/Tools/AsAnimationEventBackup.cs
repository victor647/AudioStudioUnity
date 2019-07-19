﻿using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Xml.Linq;

namespace AudioStudio
{
	public class AsAnimationEventBackup : AsSearchers
	{
		#region Fields               
		private XElement _xAnims = new XElement("Anims");
		private XElement _xModels = new XElement("Models");
		private bool _includeNonAudioEvents;
		
		private static AsAnimationEventBackup _instance;
		public static AsAnimationEventBackup Instance
		{
			get
			{
				if (!_instance)
				{
					_instance = CreateInstance<AsAnimationEventBackup>();                                       
				}
				return _instance;
			}
		}
		
		protected override string DefaultXmlPath {
			get
			{
				return AudioUtility.CombinePath(XmlDocPath, "AnimationEvents.xml");        
			}
		}
		#endregion

		protected override void CleanUp()
		{
			_xAnims = new XElement("Anims");
			_xModels = new XElement("Models");
			base.CleanUp();
		}
		
		private void OnGUI()
		{
			GUILayout.Label("This tool searches for animation clips (.anim files) and models (.fbx) files in the game and export data to an xml file. \n" +
			                "It can also import data from xml and update animation audio events into the clips and models.");

			using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
			{
				IncludeA = GUILayout.Toggle(IncludeA, "Search in Anim files");
				IncludeB = GUILayout.Toggle(IncludeB, "Search in FBX files");
				_includeNonAudioEvents = GUILayout.Toggle(_includeNonAudioEvents, "Include Non-Audio Events");
			}

			AudioScriptGUI.DisplaySearchPath(ref SearchPath);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Export to xml")) EditorApplication.delayCall += Export;
			if (GUILayout.Button("Import from xml")) EditorApplication.delayCall += Import;
			if (GUILayout.Button("Open xml")) Process.Start(DefaultXmlPath);
			GUILayout.EndHorizontal();
		}

		#region Export
		private void Export()
		{
			CleanUp();			
			CheckoutLocked(AudioUtility.CombinePath(XmlDocPath, "AnimationEvents.xml"));
			var fileName = EditorUtility.SaveFilePanel("Export to", XmlDocPath, "AnimationEvents.xml", ".xml");
			if (string.IsNullOrEmpty(fileName)) return;			
			if (IncludeA) FindFiles(ParseClip, "Exporting animation clips...", "*.anim");
			if (IncludeB) FindFiles(ParseModel, "Exporting FBX files...", "*.fbx");
			XRoot.Add(_xAnims);
			XRoot.Add(_xModels);
			AudioUtility.WriteXml(fileName, XRoot);
			EditorUtility.DisplayDialog("Success!", "Found " + TotalCount + " animation clips!", "OK");		
		}
				
		private void ParseClip(string filePath)
		{
			var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(filePath);
			if (clip == null) return;
			var xClip = new XElement("AnimationClip");									
			xClip.SetAttributeValue("Path", Path.GetDirectoryName(filePath));
			xClip.SetAttributeValue("Asset", Path.GetFileName(filePath));
			xClip.SetAttributeValue("ClipName", clip.name);
			if (_includeNonAudioEvents)
			{
				var hasEvents = false; //don't add clip node if no animation events are found
				foreach (var evt in clip.events)
				{
					hasEvents = true;
					xClip.Add(ParseEvent(evt));
				}

				if (hasEvents)
				{
					TotalCount++;
					_xAnims.Add(xClip);
				}
			}
			else
			{
				var hasAudioEvents = false;
				foreach (var evt in clip.events)
				{
					if (AudioUtility.IsSoundAnimationEvent(evt))				
					{
						hasAudioEvents = true;
						xClip.Add(ParseEvent(evt));					
					}
				}
				if (hasAudioEvents)
				{
					TotalCount++;
					_xAnims.Add(xClip);
				}
			}
		}

		private XElement ParseEvent(AnimationEvent evt)
		{
			var ae = new XElement("AnimationEvent");
			ae.SetAttributeValue("Function", evt.functionName);
			ae.SetAttributeValue("AudioEvent", evt.stringParameter);
			ae.SetAttributeValue("Time", evt.time);
			return ae;
		}
		
		private void ParseModel(string filePath)
		{
			var modelImporter = AssetImporter.GetAtPath(filePath) as ModelImporter;
			if (modelImporter == null) return;		
			
			var xModel = new XElement("Model");			
			xModel.SetAttributeValue("Path", Path.GetDirectoryName(filePath));
			xModel.SetAttributeValue("Asset", Path.GetFileName(filePath));
			
			var found = false;
			foreach (var clip in modelImporter.clipAnimations)
			{			
				var xClip = new XElement("AnimationClip");
				xClip.SetAttributeValue("ClipName", clip.name);
				var hasEvents = false;
				foreach (var evt in clip.events)
				{
					if (_includeNonAudioEvents || AudioUtility.IsSoundAnimationEvent(evt))
					{
						hasEvents = true;
						xClip.Add(ParseEvent(evt));	
					}					
				}					
				if (hasEvents)
				{			
					xModel.Add(xClip);
					TotalCount++;						
					found = true;
				}
			}

			if (found) _xModels.Add(xModel);
		}
		#endregion
		
		#region Revert
		public void RevertClip(Object selectedObject)
		{
			var clip = selectedObject as AnimationClip;
			if (clip)
				RevertAnimationClip(clip);
			else							
				RevertModel(AssetDatabase.GetAssetPath(selectedObject));			
		}

		private void RevertAnimationClip(AnimationClip clip)
		{
			LoadOrCreateXmlDoc();
			var xa = XRoot.Element("Anims");
			if (xa == null) return;
			var xClips = xa.Descendants("AnimationClip").ToList();
			var clipPath = AssetDatabase.GetAssetPath(clip);
			
			foreach (var xClip in xClips)
			{
				var fileName = AudioUtility.CombinePath(AudioUtility.GetXmlAttribute(xClip, "Path"), AudioUtility.GetXmlAttribute(xClip, "Asset"));
				if (clipPath == fileName)
					ImportClip(clip, xClip.Descendants("AnimationEvent"));
			}
		}
		
		private void RevertModel(string modelPath)
		{
			LoadOrCreateXmlDoc();
			var xm = XRoot.Element("Models");
			if (xm == null) return;
			var xModels = xm.Descendants("Model").ToList();

			foreach (var xModel in xModels)
			{
				var fileName = AudioUtility.CombinePath(AudioUtility.GetXmlAttribute(xModel, "Path"), AudioUtility.GetXmlAttribute(xModel, "Asset"));
				if (modelPath == fileName)
					ImportModel(modelPath, xModel);
			}
		}
		#endregion
		
		#region Import
		private void Import()
		{
			CleanUp();
			var fileName = EditorUtility.OpenFilePanel("Import from", XmlDocPath, "xml");
			if (string.IsNullOrEmpty(fileName)) return;
			
			XRoot = XDocument.Load(fileName).Element("Root");
			ImportFromAnims();
			ImportFromModels();
			EditorUtility.DisplayProgressBar("Saving", "Overwriting assets...(might take a few minutes)", 1f);
			AssetDatabase.SaveAssets();
			EditorUtility.ClearProgressBar();
			EditorUtility.DisplayDialog("Success!", "Updated " + EditedCount + " animation clips out of " + TotalCount, "OK");			
		}

		private void ImportFromAnims()
		{						
			var xa = XRoot.Element("Anims");
			if (xa == null) return;
			var xClips = xa.Descendants("AnimationClip").ToList();												
			TotalCount += xClips.Count;		
			var searchPath = AudioUtility.ShortPath(SearchPath);
			for (var i = 0; i < xClips.Count; i++)
			{
				var xClip = xClips[i];
				var path = AudioUtility.GetXmlAttribute(xClip, "Path");
				if (!path.Contains(searchPath)) continue;
				var fileName = AudioUtility.CombinePath(path, AudioUtility.GetXmlAttribute(xClip, "Asset"));
				var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fileName);
				if (EditorUtility.DisplayCancelableProgressBar("Modifying clips", fileName, i * 1.0f / xClips.Count)) break;
				ImportClip(clip, xClip.Descendants("AnimationEvent"));				
			}
		}		
		
		private void ImportFromModels()
		{
			var xm = XRoot.Element("Models");
			if (xm == null) return;
			var xModels = xm.Descendants("Model").ToList();						
			TotalCount += xModels.Count;			
			var searchPath = AudioUtility.ShortPath(SearchPath);
			for (var i = 0; i < xModels.Count; i++)
			{
				var xModel = xModels[i];
				var path = AudioUtility.GetXmlAttribute(xModel, "Path");
				if (!path.Contains(searchPath)) continue;
				var fileName = AudioUtility.CombinePath(path, AudioUtility.GetXmlAttribute(xModel, "Asset"));				
				if (EditorUtility.DisplayCancelableProgressBar("Modifying models", fileName, i * 1.0f / xModels.Count)) break;
				ImportModel(fileName, xModel);				
			}
		}

		private void ImportModel(string fileName, XElement xModel)
		{			
			var modelImporter = AssetImporter.GetAtPath(fileName) as ModelImporter;
			if (modelImporter == null) return;
			
			var modified = false;
			var newClips = new List<ModelImporterClipAnimation>(); 
			foreach (var clip in modelImporter.clipAnimations)
			{
				foreach (var xClip in xModel.Descendants("AnimationClip"))
				{
					if (AudioUtility.GetXmlAttribute(xClip, "ClipName") != clip.name) continue;
					if (ImportClip(clip, xClip.Descendants("AnimationEvent"))) modified = true;
				}					
				newClips.Add(clip);
			}

			if (modified)
			{
				modelImporter.clipAnimations = newClips.ToArray();
				EditorUtility.SetDirty(modelImporter);
				AssetDatabase.ImportAsset(fileName);
			}
		}
		
		private void ImportClip(AnimationClip clip, IEnumerable<XElement> xEvents)
        {
            var events = _includeNonAudioEvents ? new List<AnimationEvent>() : clip.events.Where(evt => !AudioUtility.IsSoundAnimationEvent(evt)).ToList();                        	                                    
            GenerateEventList(events, xEvents);
            if (events.Count != clip.events.Length)            
	            AnimationUtility.SetAnimationEvents(clip, events.ToArray());    	                        						
            if (events.Where((t, i) => !CompareAnimationEvent(clip.events[i], t)).Any())            
	            AnimationUtility.SetAnimationEvents(clip, events.ToArray());    	                                    
        }
		
		private bool ImportClip(ModelImporterClipAnimation clip, IEnumerable<XElement> xEvents)
		{
			var events = _includeNonAudioEvents ? new List<AnimationEvent>() : clip.events.Where(evt => !AudioUtility.IsSoundAnimationEvent(evt)).ToList();
			GenerateEventList(events, xEvents);
			if (events.Count != clip.events.Length)
			{
				EditedCount++;
				clip.events = events.ToArray();
				return true;
			}						
			if (events.Where((t, i) => !CompareAnimationEvent(clip.events[i], t)).Any())
			{
				EditedCount++;
				clip.events = events.ToArray();
				return true;
			}
			return false;
		}

		private void GenerateEventList(ICollection<AnimationEvent> events, IEnumerable<XElement> xEvents)
		{
			foreach (var e in xEvents)
			{
				var animEvent = new AnimationEvent
				{
					time = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(e, "Time")),
					stringParameter = AudioUtility.GetXmlAttribute(e, "AudioEvent"),
					functionName = AudioUtility.GetXmlAttribute(e, "Function")
				};
				events.Add(animEvent);
			}
		}

		private static bool CompareAnimationEvent(AnimationEvent a, AnimationEvent b)
		{			
			return a.time == b.time && a.stringParameter == b.stringParameter && a.functionName == b.functionName;
		}
		#endregion
    }  	
}