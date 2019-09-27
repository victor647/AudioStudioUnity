using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AudioStudio.Components;
using AudioStudio.Configs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using AudioPlayableAsset = AudioStudio.Components.AudioPlayableAsset;

namespace AudioStudio.Tools
{
	public partial class AsComponentBackup
	{
        private void OnEnable()
        {                                    
            RegisterComponent<AnimationSound>();
            RegisterComponent<AudioTag>(AudioTagImporter, AudioTagExporter);      
            RegisterComponent<ButtonSound>(ButtonSoundImporter, ButtonSoundExporter);
            RegisterComponent<ColliderSound>(ColliderSoundImporter, ColliderSoundExporter);
            RegisterComponent<DropdownSound>(DropdownSoundImporter, DropdownSoundExporter);
            RegisterComponent<EffectSound>(EffectSoundImporter, EffectSoundExporter);
            RegisterComponent<EmitterSound>(EmitterSoundImporter, EmitterSoundExporter);
            RegisterComponent<LoadBank>(LoadBankImporter, LoadBankExporter);
            RegisterComponent<MenuSound>(MenuSoundImporter, MenuSoundExporter);
            RegisterComponent<ScrollSound>(ScrollSoundImporter, ScrollSoundExporter);            
            RegisterComponent<SliderSound>(SliderSoundImporter, SliderSoundExporter);
            RegisterComponent<ToggleSound>(ToggleSoundImporter, ToggleSoundExporter);
            RegisterComponent<SetSwitch>(SetSwitchImporter, SetSwitchExporter);
        }

        #region TypeCast
        private static bool ImportFloat(ref float field, string s)
		{
			var value = AudioUtility.StringToFloat(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}

		private static bool ImportBool(ref bool field, string s)
		{
			var value = AudioUtility.StringToBool(s);
			if (field != value)
			{
				field = value;
				return true;
			}
			return false;
		}

		private static bool ImportEnum<T>(ref T field, string node) where T: struct, IComparable
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
		
		#region ImportAudioObjects                               		
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

        private static bool ImportEvent(ref AudioEventReference audioEvent, XElement node)
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

        private static bool ImportEvent(ref AudioEventReference audioEvent, XElement node, string trigger)
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

        private static bool ImportEvents(ref AudioEventReference[] audioEvents, XElement node, string trigger = "")
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

        private static bool ImportBanks(ref SoundBankReference[] banks, XElement node)
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

        private static bool ImportSwitches(ref SetSwitchReference[] switches, XElement node, string trigger = "")
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

        private static bool ImportParameter(ref AudioParameterReference parameter, out float valueScale, XElement node)
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

        private static bool ImportPhysicsSettings(AudioPhysicsHandler aph, XElement node)
        {
	        var xSettings = node.Element("PhysicsSettings");
	        var modified = ImportEnum(ref aph.MatchTags, AudioUtility.GetXmlAttribute(xSettings, "MatchTags"));
	        modified |= ImportEnum(ref aph.SetOn, AudioUtility.GetXmlAttribute(xSettings, "SetOn"));
	        modified |= ImportEnum(ref aph.PostFrom, AudioUtility.GetXmlAttribute(xSettings, "PostFrom"));
	        return modified;
        }         
        #endregion
        
        #region ExportAudioObjects
        private static void ExportPhysicsSettings(AudioPhysicsHandler component, XElement node)
        {
	        var xSettings = new XElement("PhysicsSettings");
	        xSettings.SetAttributeValue("SetOn", component.SetOn);
	        xSettings.SetAttributeValue("PostFrom", component.PostFrom);  
	        xSettings.SetAttributeValue("MatchTags", component.MatchTags);                        
	        node.Add(xSettings);
        }

        private static void ExportSwitches(SetSwitchReference[] switches, XElement node, string trigger)
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
        
        private static void ExportParameter(AudioParameterReference parameter, XElement node)
        {
	        if (!parameter.IsValid()) return;
            var xParam = new XElement("Parameter");
            xParam.SetAttributeValue("ParameterName", parameter);            
            node.Add(xParam);
        }

        private static void ExportBanks(SoundBankReference[] banks, XElement node)
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

        private static void ExportEvent(AudioEventReference audioEvent, XElement node, string trigger = "")
        {            
            if (!audioEvent.IsValid()) return;
            var xEvent = new XElement("AudioEvent");
            xEvent.SetAttributeValue("Trigger", trigger);
            xEvent.SetAttributeValue("EventType", audioEvent.EventType);
            xEvent.SetAttributeValue("EventName", audioEvent.Name);                 
            node.Add(xEvent);            
        }

        private static void ExportAudioEvents(AudioEventReference[] events, XElement node, string trigger = "")
        {            
            if (events == null) return;
            foreach (var evt in events)
            {
                ExportEvent(evt, node, trigger);
            }
        }
        #endregion
        
        #region Exporters               
        private static void AudioTagExporter(Component component, XElement node)
        {
            var s = (AudioTag) component; 
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("Tags", s.Tags); 
            node.Add(xSettings);
        }
        
        private static void ButtonSoundExporter(Component component, XElement node)
        {
            var s = (ButtonSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.ClickEvents, xEvents, "Click");
            ExportEvent(s.PointerEnterEvent, xEvents, "PointerEnter");
            ExportEvent(s.PointerExitEvent, xEvents, "PointerExit");
            node.Add(xEvents);
        }

        private static void ColliderSoundExporter(Component component, XElement node)
        {
            var s = (ColliderSound) component;            
            ExportPhysicsSettings(s, node);            
            ExportParameter(s.CollisionForceParameter, node);
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.EnterEvents, xEvents, "Enter");
            ExportAudioEvents(s.ExitEvents, xEvents, "Exit");
            node.Add(xEvents);                                                          
        }

        private static void DropdownSoundExporter(Component component, XElement node)
        {
            var s = (DropdownSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.PopupEvents, xEvents, "Popup");
            ExportAudioEvents(s.ValueChangeEvents, xEvents, "ValueChange");
            ExportAudioEvents(s.CloseEvents, xEvents, "Close");
            node.Add(xEvents);
        }

        private static void EffectSoundExporter(Component component, XElement node)
        {
            var s = (EffectSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.EnableEvents, xEvents, "Enable");
            node.Add(xEvents);
        }

        private static void EmitterSoundExporter(Component component, XElement node)
        {
            var s = (EmitterSound) component;            
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("FadeInTime", s.FadeInTime);
            xSettings.SetAttributeValue("FadeOutTime", s.FadeOutTime);
            node.Add(xSettings);          
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.AudioEvents, xEvents);                        
            node.Add(xEvents);                                                  
        }
                        
        private static void LoadBankExporter(Component component, XElement node)
        {
            var s = (LoadBank) component;    
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("UnloadOnDisable", s.UnloadOnDisable);
            node.Add(xSettings);
            ExportBanks(s.Banks, node);
        }    
        
        private static void MenuSoundExporter(Component component, XElement node)
        {
            var s = (MenuSound) component;                   
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.OpenEvents, xEvents, "Open");
            ExportAudioEvents(s.CloseEvents, xEvents, "Close");
            node.Add(xEvents);                                                          
        }

        private static void SetSwitchExporter(Component component, XElement node)
        {
            var s = (SetSwitch) component;                    
            ExportPhysicsSettings(s, node);
            var xSettings = new XElement("Settings");      
            xSettings.SetAttributeValue("IsGlobal", s.IsGlobal);
            node.Add(xSettings);     
            var xSwitches = new XElement("Switches");
            ExportSwitches(s.OnSwitches, xSwitches, "On");
            ExportSwitches(s.OffSwitches, xSwitches, "Off");            
            node.Add(xSwitches);            
        }                            
        
        private static void ToggleSoundExporter(Component component, XElement node)
        {
            var s = (ToggleSound) component;                      
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.ToggleOnEvents, xEvents, "ToggleOn");
            ExportAudioEvents(s.ToggleOffEvents, xEvents, "ToggleOff");
            node.Add(xEvents);           
        }

        private static void ScrollSoundExporter(Component component, XElement node)
        {
            var s = (ScrollSound) component;                                           
            ExportEvent(s.ScrollEvent, node);            
        }

        private static void SliderSoundExporter(Component component, XElement node)
        {
            var s = (SliderSound) component;                
            ExportParameter(s.ConnectedParameter, node);
            ExportEvent(s.DragEvent, node);                                                                                                                               
        }       
        
        public static void AudioStateExporter(AudioState s, XElement node)
        {
            var xSettings = new XElement("Settings");			
            xSettings.SetAttributeValue("AudioState", s.AnimationAudioState.ToString());						            
            xSettings.SetAttributeValue("ResetStateOnExit", s.ResetStateOnExit);
            node.Add(xSettings);
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.EnterEvents, xEvents, "Enter");
            ExportAudioEvents(s.ExitEvents, xEvents, "Exit");
            node.Add(xEvents);
            var xSwitches = new XElement("Switches");
            ExportSwitches(s.EnterSwitches, xSwitches, "Enter");            
            node.Add(xSwitches);
        }

        public static void AudioPlayableAssetExporter(AudioPlayableAsset apa, TimelineClip clip, XElement node)
        {
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("StartTime", (float) clip.start);
            xSettings.SetAttributeValue("Duration", (float) clip.duration);
            node.Add(xSettings);
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(apa.StartEvents, xEvents, "Start");
            ExportAudioEvents(apa.EndEvents, xEvents, "End");
            node.Add(xEvents);
        }
        #endregion 
        
        #region Importers                
        private static bool AudioTagImporter(Component component, XElement node)
        {
            var s = (AudioTag) component;    
            var xSettings = node.Element("Settings");
            return ImportEnum(ref s.Tags, AudioUtility.GetXmlAttribute(xSettings, "Tags"));            
        }
        
        private static bool ButtonSoundImporter(Component component, XElement node)
        {
            var s = (ButtonSound) component;
            var modified = ImportEvents(ref s.ClickEvents, node, "Click");
            modified |= ImportEvent(ref s.PointerEnterEvent, node, "PointerEnter");
            modified |= ImportEvent(ref s.PointerExitEvent, node, "PointerExit");
            return modified;
        }

        private static bool ColliderSoundImporter(Component component, XElement node)
        {
            var s = (ColliderSound) component;
            var modified = ImportPhysicsSettings(s, node);            
            modified |= ImportParameter(ref s.CollisionForceParameter, out s.ValueScale, node);
            modified |= ImportEvents(ref s.EnterEvents, node, "Enter");
            modified |= ImportEvents(ref s.ExitEvents, node, "Exit");
            return modified;
        }
        
        private static bool DropdownSoundImporter(Component component, XElement node)
        {            
            var s = (DropdownSound) component;
            var modified = ImportEvents(ref s.ValueChangeEvents, node, "ValueChange");
            modified |= ImportEvents(ref s.PopupEvents, node, "Popup");
            modified |= ImportEvents(ref s.CloseEvents, node, "Close");
            return modified;
        }
    
        private static bool EffectSoundImporter(Component component, XElement node)
        {
            var s = (EffectSound) component;
            var modified = ImportEvents(ref s.EnableEvents, node, "Enable");
            return modified;
        }

        private static bool EmitterSoundImporter(Component component, XElement node)
        {
            var s = (EmitterSound) component;
            var xSettings = node.Element("Settings");                        
            var modified = ImportEvents(ref s.AudioEvents, node);
            modified |= ImportFloat(ref s.FadeInTime, AudioUtility.GetXmlAttribute(xSettings, "FadeInTime"));
            modified |= ImportFloat(ref s.FadeOutTime, AudioUtility.GetXmlAttribute(xSettings, "FadeOutTime"));            
            return modified;
        }
                        
        private static bool LoadBankImporter(Component component, XElement node)
        {
            var s = (LoadBank) component;    
            var xSettings = node.Element("Settings");
            var modified = ImportBool(ref s.UnloadOnDisable, AudioUtility.GetXmlAttribute(xSettings, "UnloadOnDisable"));
            modified |= ImportBanks(ref s.Banks, node);
            return modified;
        }    
        
        private static bool MenuSoundImporter(Component component, XElement node)
        {
            var s = (MenuSound) component;                   
            return ImportEvents(ref s.OpenEvents, node, "Open") ||            
                   ImportEvents(ref s.CloseEvents, node, "Close");            
        }

        private static bool SetSwitchImporter(Component component, XElement node)
        {
            var s = (SetSwitch) component;                    
            var modified = ImportPhysicsSettings(s, node);
            var xSettings = node.Element("Settings");              
            modified |= ImportBool(ref s.IsGlobal, AudioUtility.GetXmlAttribute(xSettings, "IsGlobal"));
            modified |= ImportSwitches(ref s.OnSwitches, node, "On");
            modified |= ImportSwitches(ref s.OffSwitches, node, "Off");                           
            return modified;           
        }                            
        
        private static bool ToggleSoundImporter(Component component, XElement node)
        {
            var s = (ToggleSound) component;
            var modified = ImportEvents(ref s.ToggleOnEvents, node, "ToggleOn");
            modified |= ImportEvents(ref s.ToggleOffEvents, node, "ToggleOff");
            return modified;
        }

        private static bool ScrollSoundImporter(Component component, XElement node)
        {
            var s = (ScrollSound) component;                                           
            return ImportEvent(ref s.ScrollEvent, node);            
        }

        private static bool SliderSoundImporter(Component component, XElement node)
        {
            var s = (SliderSound) component;
            var modified = ImportParameter(ref s.ConnectedParameter, out s.ValueScale, node);
            modified |= ImportEvent(ref s.DragEvent, node);
            return modified;
        }     
        
        public static bool AudioStateImporter(AudioState audioState, XElement node)
        {
            var xSettings = node.Element("Settings");
            var modified = ImportEnum(ref audioState.AnimationAudioState, AudioUtility.GetXmlAttribute(xSettings, "AudioState"));						            
            modified |= ImportBool(ref audioState.ResetStateOnExit, AudioUtility.GetXmlAttribute(xSettings, "ResetStateOnExit"));
            modified |= ImportEvents(ref audioState.EnterEvents, node, "Enter");
            modified |= ImportEvents(ref audioState.ExitEvents, node, "Exit");
            modified |= ImportSwitches(ref audioState.EnterSwitches, node, "Enter");
            return modified;
        }
        
        public static bool AudioPlayableAssetImporter(AudioPlayableAsset apa, TimelineClip clip, XElement node)
        {
            var xSettings = node.Element("Settings");
            var clipName = AudioUtility.GetXmlAttribute(node, "ClipName");
            var modified = false;
            var start = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(xSettings, "StartTime"));
            if (clip.displayName != clipName)
            {
                clip.displayName = clipName;
                modified = true;
            }
            if (clip.start != start)
            {
                clip.start = start;
                modified = true;
            }
            var duration = AudioUtility.StringToFloat(AudioUtility.GetXmlAttribute(xSettings, "Duration"));
            if (clip.duration != duration)
            {
                clip.duration = duration;
                modified = true;
            }
            modified |= ImportEvents(ref apa.StartEvents, node, "Start");
            modified |= ImportEvents(ref apa.EndEvents, node, "End");   
 
            return modified;
        }
        #endregion
        
        #region Refreshers
        public static void RefreshEvent(AudioEventReference evt)
        {
	        var assets = AssetDatabase.FindAssets(evt.Name);
	        if (!assets.Contains(evt.Name))
		        evt = new AudioEventReference();
        }
        
        public static void RefreshParameter(AudioParameterReference parameter)
        {
	        var assets = AssetDatabase.FindAssets(parameter.Name);
	        if (!assets.Contains(parameter.Name))
		        parameter = new AudioParameterReference();
        }
        
        public static void RefreshSwitch(AudioSwitchReference swc)
        {
	        var assets = AssetDatabase.FindAssets(swc.Name);
	        if (!assets.Contains(swc.Name))
		        swc = new AudioSwitchReference();
        }
        
        public static void RefreshBank(SoundBankReference bank)
        {
	        var assets = AssetDatabase.FindAssets(bank.Name);
	        if (!assets.Contains(bank.Name))
		        bank = new SoundBankReference();
        }
        #endregion
	}
}
