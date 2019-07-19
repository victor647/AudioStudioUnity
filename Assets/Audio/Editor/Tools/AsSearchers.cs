using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace AudioStudio
{
	public enum XmlAction
	{
		Save,
		Revert,
		Remove
	}
	
	public abstract class AsSearchers : EditorWindow
	{
		protected bool IncludeInactive = true;
		protected bool IncludeA = true;
		protected bool IncludeB = true;
		protected string SearchPath;
		protected string XmlDocPath;
		protected int EditedCount;
		protected int TotalCount;
		protected XElement XRoot = new XElement("Root");

		protected virtual string DefaultXmlPath
		{
			get { return ""; }
		}

		protected virtual void OnEnable()
		{
			CleanUp();
			SearchPath = Application.dataPath;
			XmlDocPath = Path.Combine(AudioPathSettings.AudioStudioLibraryPathFull, "Editor/Configs");
		}

		protected virtual void CleanUp()
		{
			XRoot = new XElement("Root");
			TotalCount = 0;
			EditedCount = 0;
		}

		protected void LoadOrCreateXmlDoc()
		{
			XRoot = XDocument.Load(DefaultXmlPath).Element("Root");
			if (XRoot == null)
			{
				XRoot = new XElement("Root");				
				AudioUtility.WriteXml(DefaultXmlPath, XRoot);
			}
		}

		protected bool FindFiles(Action<string> parser, string progressBarTitle, string extension)
		{
			try
			{
				EditorUtility.DisplayCancelableProgressBar(progressBarTitle, "Loading assets...", 0);
				string[] allFiles = Directory.GetFiles(SearchPath, extension, SearchOption.AllDirectories);
				for (var i = 0; i < allFiles.Length; i++)
				{
					var shortPath = AudioUtility.ShortPath(allFiles[i]);
					parser(shortPath);
					if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, shortPath, (i + 1) * 1.0f / allFiles.Length)) return false;
				}

				EditorUtility.ClearProgressBar();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				EditorUtility.ClearProgressBar();
			}

			return true;
		}

		protected static GameObject GetRootGameObject(Transform trans)
		{
			return trans.parent ? GetRootGameObject(trans.parent) : trans.gameObject;
		}
				
		protected static string GetFullAssetPath(XElement node)
		{
			return AudioUtility.CombinePath(AudioUtility.GetXmlAttribute(node, "Path"), AudioUtility.GetXmlAttribute(node, "Asset"));
		}
		
		public static string GetGameObjectPath(Transform transform)
		{
			if (transform.parent == null)
				return transform.name;
			return GetGameObjectPath(transform.parent) + "/" + transform.name;
		}

		protected static GameObject GetGameObject(GameObject go, string fullName)
		{
			if (go.name == fullName)
				return go;
			var names = fullName.Split('/');
			return go.name != names[0] ? null : GetGameObject(go, names, 1);
		}

		private static GameObject GetGameObject(GameObject go, string[] names, int index)
		{
			if (index > names.Length) return null;

			foreach (Transform child in go.transform)
			{
				if (child.gameObject.name == names[index])
				{
					if (index == names.Length - 1) return child.gameObject;
					return GetGameObject(child.gameObject, names, index + 1);
				}
			}

			return null;
		}

		protected static void CheckoutLocked(string filePath)
		{
			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists) return;			
			if (fileInfo.IsReadOnly)
			{
				if (Provider.isActive)
					Provider.Checkout(filePath, CheckoutMode.Asset);					
				fileInfo.IsReadOnly = false;
			}
		}
		
		#region FieldImporters
		protected static bool ImportString(ref string field, string s)
		{
			if (field != s)
			{
				field = s;
				return true;
			}
			return false;
		}

		protected static bool ImportFloat(ref float field, string s)
		{
			var value = AudioUtility.StringToFloat(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}

		protected static bool ImportBool(ref bool field, string s)
		{
			var value = AudioUtility.StringToBool(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}

		protected static bool ImportVector3(ref Vector3 field, string s)
		{
			var value = AudioUtility.StringToVector3(s);
			if (Mathf.Abs(field.magnitude - value.magnitude) > 0.01f)
			{
				field = value;
				return true;
			}
			return false;
		}

		protected static bool ImportVector3List(ref List<Vector3> vectors, XElement node)
		{
			var positionStrings = AudioUtility.GetXmlAttribute(node, "Positions").Split('/');
			var newPositions = new List<Vector3>();
			for (var i = 0; i < positionStrings.Length - 1; i++)
			{				
				newPositions.Add(AudioUtility.StringToVector3(positionStrings[i]));
			}

			if (newPositions.Count != vectors.Count)
			{
				vectors = newPositions;
				return true;
			}
			for (var i = 0; i < vectors.Count; i++)
			{
				if (Mathf.Abs(newPositions[i].magnitude - vectors[i].magnitude) > 0.01f)
				{
					vectors = newPositions;
					return true;
				}
			}        
			return false;
		} 
		
		protected static bool ImportEnum<T>(ref T field, string node) where T: struct, IComparable
		{
			try
			{
				var value = (T) Enum.Parse(typeof(T), node);
				if (!field.Equals(value))
				{
					field = value;
					return true;
				}
			}
#pragma warning disable 168
			catch (Exception e)
#pragma warning restore 168
			{
				Debug.LogError("Import failed: Can't find option " + node + " in enum " + typeof(T).Name);
			}
			return false;
		}				
		#endregion		
		
		#region ImportFields                               		
		private static AudioEventReference XmlToEvent(XElement node)
		{
			var name = AudioUtility.GetXmlAttribute(node, "EventName");                    
			return !string.IsNullOrEmpty(name) ? new AudioEventReference{Name = name} : null;
		}
        
        private static IEnumerable<XElement> GetXmlAudioEvents(XElement node)
        {
	        var n = node.Element("AudioEvents");
	        return n?.Descendants("AudioEvent");
        }

        protected static bool ImportEvent(ref AudioEventReference audioEvent, XElement node)
        {
	        var x = node.Element("AudioEvent");
	        if (x == null)
	        {
		        if (!audioEvent.IsValid()) return false;
		        audioEvent = new AudioEventReference();
		        return true;
	        }
	        var temp = XmlToEvent(x);                           
	        if (!audioEvent.Equals(temp))
	        {
		        audioEvent = temp;
		        return true;
	        }
	        return false;
        }
        
        protected static bool ImportEvent(ref AudioEventReference audioEvent, XElement node, string trigger)
        {
	        var xEvents = GetXmlAudioEvents(node);  
	        var temp = new AudioEventReference();          
	        foreach (var xEvent in xEvents)
	        {
		        if (AudioUtility.GetXmlAttribute(xEvent, "Trigger") == trigger)
		        {
			        temp = XmlToEvent(xEvent);
			        break;
		        }                                                                                                                                  
	        }            
	        if (!audioEvent.Equals(temp))
	        {
		        audioEvent = temp;
		        return true;
	        }
	        return false;
        }

        protected static bool ImportEvents(ref AudioEventReference[] audioEvents, XElement node, string trigger = "")
        {            
            var xEvents = GetXmlAudioEvents(node);
            var audioEventsTemp = (from xEvent in xEvents where AudioUtility.GetXmlAttribute(xEvent, "Trigger") == trigger select XmlToEvent(xEvent)).ToList();
            if (!audioEvents.ToList().SequenceEqual(audioEventsTemp))
            {
                audioEvents = audioEventsTemp.ToArray();
                return true;
            }                         
            return false;
        }

        private static SoundBankReference XmlToBank(XElement node)
        {
	        var name = AudioUtility.GetXmlAttribute(node, "BankName");                    
	        return !string.IsNullOrEmpty(name) ? new SoundBankReference{Name = name} : null;
        }
        
        protected static bool ImportBanks(ref SoundBankReference[] banks, XElement node)
        {
	        var bs = node.Element("Banks");
	        if (bs == null) return false; 
	        var xBanks = bs.Descendants("Bank");            
	        var banksTemp = xBanks.Select(XmlToBank).ToList();
	        if (!banks.ToList().SequenceEqual(banksTemp))
	        {
		        banks = banksTemp.ToArray();
		        return true;
	        }
	        return false;
        }
        
        private static SetSwitchReference XmlToSwitch(XElement node)
        {
	        var fullName = AudioUtility.GetXmlAttribute(node, "SwitchName");
	        var groupName = fullName.Split('/')[0].Trim();
	        var switchName = fullName.Split('/')[1].Trim();	 
	        if (!string.IsNullOrEmpty(groupName) && !string.IsNullOrEmpty(switchName))
		        return new SetSwitchReference{Name = groupName, Selection = switchName};	              
	        return null;
        }
        
        protected static bool ImportSwitches(ref SetSwitchReference[] switches, XElement node, string trigger = "")
        {            
	        var ss = node.Element("Switches");
	        if (ss == null) return false;
	        var xSwitches = ss.Descendants("Switch");            
	        var switchesTemp = new List<SetSwitchReference>();
	        foreach (var xSwitch in xSwitches)
	        {
		        if (trigger != "")
		        {
			        if (AudioUtility.GetXmlAttribute(xSwitch, "Trigger") == trigger)
				        switchesTemp.Add(XmlToSwitch(xSwitch));
		        }
		        else
		        {
			        switchesTemp.Add(XmlToSwitch(xSwitch));
		        }                                                                                                                     
	        }
	        if (!switches.ToList().SequenceEqual(switchesTemp))
	        {
		        switches = switchesTemp.ToArray();
		        return true;
	        }                         
	        return false;
        }
        
        private static AudioParameterReference XmlToParameter(XElement node)
        {
	        var name = AudioUtility.GetXmlAttribute(node, "ParameterName");                  	        
	        return !string.IsNullOrEmpty(name) ? new AudioParameterReference{Name = name} : null;
        }
        
        protected static bool ImportParameter(ref AudioParameterReference parameter, out float valueScale, XElement node)
        {
	        var x = node.Element("Parameter");
	        if (x == null)
	        {
		        valueScale = 1f;
		        if (!parameter.IsValid()) return false;
		        parameter = new AudioParameterReference();
		        return true;
	        }
	        valueScale = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(x, "ValueScale"));
	        var temp = XmlToParameter(x);       
	        if (!parameter.Equals(temp))
	        {
		        parameter = temp;
		        return true;
	        }
	        return false;
        }
        
        protected static bool ImportPhysicsSettings(AudioPhysicsHandler aph, XElement node)
        {
	        var xSettings = node.Element("PhysicsSettings");
	        var modified = ImportEnum(ref aph.MatchTags, AudioUtility.GetXmlAttribute(xSettings, "MatchTags"));
	        modified |= ImportEnum(ref aph.SetOn, AudioUtility.GetXmlAttribute(xSettings, "SetOn"));
	        modified |= ImportEnum(ref aph.PostFrom, AudioUtility.GetXmlAttribute(xSettings, "PostFrom"));
	        return modified;
        }         
        #endregion
        
        #region ExportFields
        protected static void ExportPhysicsSettings(AudioPhysicsHandler component, XElement node)
        {
	        var xSettings = new XElement("PhysicsSettings");
	        xSettings.SetAttributeValue("SetOn", component.SetOn);
	        xSettings.SetAttributeValue("PostFrom", component.PostFrom);  
	        xSettings.SetAttributeValue("MatchTags", component.MatchTags);                        
	        node.Add(xSettings);
        }

        protected static void ExportSwitches(SetSwitchReference[] switches, XElement node, string trigger)
        {            
	        if (switches == null) return;
	        foreach (var swc in switches)
	        {
		        if (!swc.IsValid()) continue;
		        var xState = new XElement("Switch"); 
		        xState.SetAttributeValue("Trigger", trigger);
		        xState.SetAttributeValue("SwitchName", swc.FullName);		         
		        node.Add(xState);  
	        }            
        }
        
        protected static void ExportParameter(AudioParameterReference parameter, XElement node)
        {
	        if (!parameter.IsValid()) return;
            var xParam = new XElement("Parameter");
            xParam.SetAttributeValue("ParameterName", parameter);            
            node.Add(xParam);
        }

        protected static void ExportBanks(SoundBankReference[] banks, XElement node)
        {
            if (banks == null) return;
            var xBanks = new XElement("Banks");
            foreach (var bank in banks)
            {
	            if (!bank.IsValid()) continue;
                var xBank = new XElement("Bank");                
                xBank.SetAttributeValue("BankName", bank.Name);                   
                xBanks.Add(xBank);  
            }
            node.Add(xBanks);
        }

        protected static void ExportEvent(AudioEventReference audioEvent, XElement node, string trigger = "")
        {            
            if (!audioEvent.IsValid()) return;
            var xEvent = new XElement("AudioEvent");
            xEvent.SetAttributeValue("Trigger", trigger);
            xEvent.SetAttributeValue("EventType", audioEvent.EventType);
            xEvent.SetAttributeValue("EventName", audioEvent.Name);                 
            node.Add(xEvent);            
        }

        protected static void ExportEvents(AudioEventReference[] events, XElement node, string trigger = "")
        {            
            if (events == null) return;
            foreach (var evt in events)
            {
                ExportEvent(evt, node, trigger);
            }
        }
        #endregion
	}		
}