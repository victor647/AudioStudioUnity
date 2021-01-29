using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using AudioStudio.Components;
using AudioStudio.Configs;
using AudioStudio.Timeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace AudioStudio.Tools
{
	internal partial class AsComponentBackup
	{
        private void OnEnable()
        {                                    
            RegisterComponent<AnimationSound>(ImportSpatialSettings, ExportSpatialSettings);
            RegisterComponent<AudioTag>(AudioTagImporter, AudioTagExporter);      
            RegisterComponent<AudioListener3D>(AudioListener3DImporter, AudioListener3DExporter);
            RegisterComponent<ButtonSound>(ButtonSoundImporter, ButtonSoundExporter);
            RegisterComponent<ColliderSound>(ColliderSoundImporter, ColliderSoundExporter);
            RegisterComponent<DropdownSound>(DropdownSoundImporter, DropdownSoundExporter);
            RegisterComponent<EffectSound>(EffectSoundImporter, EffectSoundExporter);
            RegisterComponent<EventSound>(EventSoundImporter, EventSoundExporter);
            RegisterComponent<EmitterSound>(EmitterSoundImporter, EmitterSoundExporter);
            RegisterComponent<LegacyAnimationSound>(LegacyAnimationSoundImporter, LegacyAnimationSoundExporter);
            RegisterComponent<LoadBank>(LoadBankImporter, LoadBankExporter);
            RegisterComponent<MenuSound>(MenuSoundImporter, MenuSoundExporter);
            RegisterComponent<ScrollSound>(ScrollSoundImporter, ScrollSoundExporter);            
            RegisterComponent<SliderSound>(SliderSoundImporter, SliderSoundExporter);
            RegisterComponent<TimelineSound>(TimelineSoundImporter, TimelineSoundExporter);
            RegisterComponent<ToggleSound>(ToggleSoundImporter, ToggleSoundExporter);
            RegisterComponent<SetSwitch>(SetSwitchImporter, SetSwitchExporter);
        }

        #region ImportAudioObjects    
		private static AudioEventReference XmlToAudioEvent(XElement xEvent)
		{
			var name = AsScriptingHelper.GetXmlAttribute(xEvent, "EventName");
			var newEvent = new AudioEventReference (name);
			ImportEnum(ref newEvent.Type, AsScriptingHelper.GetXmlAttribute(xEvent, "Type"));
			return newEvent;
		}
		
		private static PostEventReference XmlToPostEvent(XElement xEvent)
		{
			var name = AsScriptingHelper.GetXmlAttribute(xEvent, "EventName");
			var newEvent = new PostEventReference (name);
			ImportEnum(ref newEvent.Type, AsScriptingHelper.GetXmlAttribute(xEvent, "Type"));
			ImportEnum(ref newEvent.Action, AsScriptingHelper.GetXmlAttribute(xEvent, "Action"));
			ImportFloat(ref newEvent.FadeTime, AsScriptingHelper.GetXmlAttribute(xEvent, "FadeTime"));
			return newEvent;
		}

		private static IEnumerable<XElement> GetXmlAudioEvents(XElement xComponent)
        {
	        var n = xComponent.Element("AudioEvents");
	        return n?.Descendants("AudioEvent");
        }

        private static bool ImportEvent(ref PostEventReference audioEvent, XElement xComponent)
        {
	        var x = xComponent.Element("AudioEvent");
	        if (x == null)
	        {
		        if (!audioEvent.IsValid()) return false;
		        audioEvent = new PostEventReference();
		        return true;
	        }
	        var temp = XmlToPostEvent(x);                           
	        if (!audioEvent.Equals(temp))
	        {
		        audioEvent = temp;
		        return true;
	        }
	        return false;
        }

        private static bool ImportEvent(ref PostEventReference audioEvent, XElement xComponent, string trigger)
        {
	        var xEvents = GetXmlAudioEvents(xComponent);  
	        var temp = new PostEventReference();          
	        foreach (var xEvent in xEvents)
	        {
		        if (AsScriptingHelper.GetXmlAttribute(xEvent, "Trigger") == trigger)
		        {
			        temp = XmlToPostEvent(xEvent);
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
        
        private static void ImportEvents(ref AudioEventReference[] events, XElement xComponent)
        {            
	        var xEvents = GetXmlAudioEvents(xComponent);
	        if (xEvents == null) return;
	        var audioEventsTemp = (from xEvent in xEvents select XmlToAudioEvent(xEvent)).ToList();
	        if (!events.ToList().SequenceEqual(audioEventsTemp))
		        events = audioEventsTemp.ToArray();
        }

        private static bool ImportEvents(ref PostEventReference[] postEvents, XElement xComponent, string trigger = "")
        {            
            var xEvents = GetXmlAudioEvents(xComponent);
            if (xEvents == null) return false;
            var audioEventsTemp = string.IsNullOrEmpty(trigger) ? 
	            (from xEvent in xEvents select XmlToPostEvent(xEvent)).ToList() : 
	            (from xEvent in xEvents where AsScriptingHelper.GetXmlAttribute(xEvent, "Trigger") == trigger select XmlToPostEvent(xEvent)).ToList();
            if (!postEvents.ToList().SequenceEqual(audioEventsTemp))
            {
                postEvents = audioEventsTemp.ToArray();
                return true;
            }                         
            return false;
        }

        private static LoadBankReference XmlToBankRef(XElement xBank)
        {
	        var name = AsScriptingHelper.GetXmlAttribute(xBank, "BankName");
	        var bank = new LoadBankReference(name);
	        ImportBool(ref bank.UnloadOnDisable, AsScriptingHelper.GetXmlAttribute(xBank, "UnloadOnDisable"));
	        ImportEvents(ref bank.LoadFinishEvents, xBank);
	        return bank;
        }
        
        private static SoundBank XmlToBank(XElement xBank)
        {
	        var bankName = AsScriptingHelper.GetXmlAttribute(xBank, "BankName");
	        if (string.IsNullOrEmpty(bankName)) return null;
	        var bankPath = AsScriptingHelper.CombinePath("Assets", AudioPathSettings.Instance.SoundBanksPath, bankName + ".asset");
	        var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(bankPath);
	        return bank;
        }
        
        private static bool ImportSyncBanks(ref SoundBank[] banks, XElement xComponent)
        {
	        var bs = xComponent.Element("SyncBanks");
	        if (bs == null) return false; 
	        var xBanks = bs.Descendants("SoundBank");            
	        var banksTemp = xBanks.Select(XmlToBank).ToList();
	        if (!banks.ToList().SequenceEqual(banksTemp))
	        {
		        banks = banksTemp.ToArray();
		        return true;
	        }
	        return false;
        }

        private static bool ImportAsyncBankRefs(ref LoadBankReference[] banks, XElement xComponent)
        {
	        var bs = xComponent.Element("AsyncBanks");
	        if (bs == null) return false; 
	        var xBanks = bs.Descendants("SoundBank");            
	        var banksTemp = xBanks.Select(XmlToBankRef).ToList();
	        if (!banks.ToList().SequenceEqual(banksTemp))
	        {
		        banks = banksTemp.ToArray();
		        return true;
	        }
	        return false;
        }
        
        private static SetSwitchReference XmlToSwitch(XElement xSwitch)
        {
	        var fullName = AsScriptingHelper.GetXmlAttribute(xSwitch, "SwitchName");
	        var groupName = fullName.Split('/')[0].Trim();
	        var switchName = fullName.Split('/')[1].Trim();
	        return new SetSwitchReference(groupName, switchName);
        }

        private static bool ImportSwitches(ref SetSwitchReference[] switches, XElement xComponent, string trigger = "")
        {            
	        var ss = xComponent.Element("Switches");
	        if (ss == null) return false;
	        var xSwitches = ss.Descendants("Switch");            
	        var switchesTemp = new List<SetSwitchReference>();
	        foreach (var xSwitch in xSwitches)
	        {
		        if (trigger != "")
		        {
			        if (AsScriptingHelper.GetXmlAttribute(xSwitch, "Trigger") == trigger)
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
        
        private static AudioParameterReference XmlToParameter(XElement xParameter)
        {
	        var name = AsScriptingHelper.GetXmlAttribute(xParameter, "ParameterName");
	        return new AudioParameterReference(name);
        }

        private static bool ImportParameter(ref AudioParameterReference parameter, out float valueScale, XElement xComponent)
        {
	        var x = xComponent.Element("Parameter");
	        if (x == null)
	        {
		        valueScale = 1f;
		        if (!parameter.IsValid()) return false;
		        parameter = new AudioParameterReference();
		        return true;
	        }
	        valueScale = AsScriptingHelper.StringToFloat(AsScriptingHelper.GetXmlAttribute(x, "ValueScale"));
	        var temp = XmlToParameter(x);       
	        if (!parameter.Equals(temp))
	        {
		        parameter = temp;
		        return true;
	        }
	        return false;
        }

        private static bool ImportTriggerSettings(AsTriggerHandler aph, XElement xComponent)
        {
	        var xSettings = xComponent.Element("TriggerSettings");
	        var modified = ImportEnum(ref aph.MatchTags, AsScriptingHelper.GetXmlAttribute(xSettings, "MatchTags"));
	        modified |= ImportEnum(ref aph.SetOn, AsScriptingHelper.GetXmlAttribute(xSettings, "SetOn"));
	        modified |= ImportEnum(ref aph.PostFrom, AsScriptingHelper.GetXmlAttribute(xSettings, "PostFrom"));
	        return modified;
        }         
        
        private static bool ImportSpatialSettings(Component component, XElement xComponent)
        {
	        var emitter = (AudioEmitter3D) component;
	        var xSettings = xComponent.Element("SpatialSettings");
	        var modified = ImportBool(ref emitter.IsUpdatePosition, AsScriptingHelper.GetXmlAttribute(xSettings, "IsUpdatePosition"));
	        modified |= ImportBool(ref emitter.StopOnDestroy, AsScriptingHelper.GetXmlAttribute(xSettings, "StopOnDestroy"));
	        return modified;
        }
        
        private static bool ImportGameObjects(GameObject parent, ref GameObject[] gameObjects, XElement xGameObjects)
        {
	        var gameObjectsTemp = xGameObjects.Elements("GameObject").Select
		        (xGameObject => GetGameObject(parent, AsScriptingHelper.GetXmlAttribute(xGameObject, "Path"))).Where(gameObject => gameObject).ToList();
	        if (!gameObjects.ToList().SequenceEqual(gameObjectsTemp))
	        {
		        gameObjects = gameObjectsTemp.ToArray();
		        return true;
	        }
	        return false;
        }
        #endregion
        
        #region ExportAudioObjects
        private static string ExportVector3(Vector3 vector)
        {
	        return vector.x + ", " + vector.y + ", " + vector.z;
        }
        
        private static void ExportGameObjects(GameObject parent, GameObject[] gameObjects, XElement xGameObjects)
        {
	        foreach (var gameObject in gameObjects)
	        {
		        if (!gameObject) continue;
		        var xGameObject = new XElement("GameObject"); 
		        xGameObject.SetAttributeValue("Path", GetGameObjectPath(gameObject.transform, parent.transform));
		        xGameObjects.Add(xGameObject);
	        }
        }
        
        private static void ExportSpatialSettings(Component component, XElement xComponent)
        {
	        var emitter = (AudioEmitter3D) component;
	        var xSettings = new XElement("SpatialSettings");
	        xSettings.SetAttributeValue("IsUpdatePosition", emitter.IsUpdatePosition);
	        xSettings.SetAttributeValue("StopOnDestroy", emitter.StopOnDestroy);
	        xComponent.Add(xSettings);
        }
        
        private static void ExportTriggerSettings(AsTriggerHandler component, XElement xComponent)
        {
	        var xSettings = new XElement("TriggerSettings");
	        xSettings.SetAttributeValue("SetOn", component.SetOn);
	        xSettings.SetAttributeValue("PostFrom", component.PostFrom);  
	        xSettings.SetAttributeValue("MatchTags", component.MatchTags);                        
	        xComponent.Add(xSettings);
        }

        private static void ExportSwitches(SetSwitchReference[] switches, XElement xComponent, string trigger)
        {            
	        if (switches == null) return;
	        foreach (var swc in switches)
	        {
		        if (!swc.IsValid()) continue;
		        var xState = new XElement("Switch"); 
		        xState.SetAttributeValue("Trigger", trigger);
		        xState.SetAttributeValue("SwitchName", swc.FullName);		         
		        xComponent.Add(xState);  
	        }            
        }
        
        private static void ExportParameter(AudioParameterReference parameter, XElement xComponent)
        {
	        if (!parameter.IsValid()) return;
            var xParam = new XElement("Parameter");
            xParam.SetAttributeValue("ParameterName", parameter);            
            xComponent.Add(xParam);
        }
        
        private static void ExportSyncBanks(SoundBank[] banks, XElement xComponent)
        {
	        if (banks == null) return;
	        var xBanks = new XElement("SyncBanks");
	        foreach (var bank in banks)
	        {
		        var xBank = new XElement("SoundBank");
		        xBank.SetAttributeValue("BankName", bank.name);
		        xBanks.Add(xBank);
	        }
	        xComponent.Add(xBanks);
        }

        private static void ExportAsyncBankRefs(LoadBankReference[] banks, XElement xComponent)
        {
	        if (banks == null) return;
	        var xBanks = new XElement("AsyncBanks");
	        foreach (var bank in banks)
	        {
		        if (!bank.IsValid()) continue;
		        var xBank = new XElement("SoundBank");                
		        xBank.SetAttributeValue("BankName", bank.Name);
		        xBank.SetAttributeValue("UnloadOnDisable", bank.UnloadOnDisable);
		        var xEvents = new XElement("AudioEvents");
		        if (bank.LoadFinishEvents != null)
		        {
			        foreach (var evt in bank.LoadFinishEvents)
			        {
				        var xEvent = new XElement("AudioEvent");
				        xEvent.SetAttributeValue("Type", evt.Type);
				        xEvent.SetAttributeValue("EventName", evt.Name);
				        xEvents.Add(xEvent);
			        }
		        }
		        xBank.Add(xEvents);
		        xBanks.Add(xBank);  
	        }
	        xComponent.Add(xBanks);
        }

        private static void ExportEvent(PostEventReference audioEvent, XElement xEvents, string trigger = "")
        {            
            if (!audioEvent.IsValid()) return;
            var xEvent = new XElement("AudioEvent");
            if (!string.IsNullOrEmpty(trigger))
				xEvent.SetAttributeValue("Trigger", trigger);
            xEvent.SetAttributeValue("Type", audioEvent.Type);
            xEvent.SetAttributeValue("EventName", audioEvent.Name);
            xEvent.SetAttributeValue("Action", audioEvent.Action);
            xEvent.SetAttributeValue("FadeTime", audioEvent.FadeTime);
            xEvents.Add(xEvent);
        }

        private static void ExportAudioEvents(PostEventReference[] events, XElement xEvents, string trigger = "")
        {            
            if (events == null) return;
            foreach (var evt in events)
            {
                ExportEvent(evt, xEvents, trigger);
            }
        }
        #endregion
        
        #region Exporters               
        private static void AudioTagExporter(Component component, XElement xComponent)
        {
            var s = (AudioTag) component; 
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("Tags", s.Tags); 
            xComponent.Add(xSettings);
        }
        
        private static void AudioListener3DExporter(Component component, XElement xComponent)
        {
	        var s = (AudioListener3D) component;
	        var xSettings = new XElement("Settings");
	        xSettings.SetAttributeValue("PositionOffset", ExportVector3(s.PositionOffset));
	        xSettings.SetAttributeValue("MoveZAxisByCameraFOV", s.MoveZAxisByCameraFOV);
	        xSettings.SetAttributeValue("MinFOV", s.MinFOV);
	        xSettings.SetAttributeValue("MaxFOV", s.MaxFOV);
	        xSettings.SetAttributeValue("MinOffset", s.MinOffset);
	        xSettings.SetAttributeValue("MaxOffset", s.MaxOffset);
	        xComponent.Add(xSettings);
        }

        private static void ButtonSoundExporter(Component component, XElement xComponent)
        {
            var s = (ButtonSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.ClickEvents, xEvents, "Click");
            ExportEvent(s.PointerEnterEvent, xEvents, "PointerEnter");
            ExportEvent(s.PointerExitEvent, xEvents, "PointerExit");
            xComponent.Add(xEvents);
        }

        private static void ColliderSoundExporter(Component component, XElement xComponent)
        {
            var s = (ColliderSound) component;           
            ExportSpatialSettings(s, xComponent);
            ExportTriggerSettings(s, xComponent);            
            ExportParameter(s.CollisionForceParameter, xComponent);
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.EnterEvents, xEvents, "Enter");
            ExportAudioEvents(s.ExitEvents, xEvents, "Exit");
            xComponent.Add(xEvents);                                                          
        }

        private static void DropdownSoundExporter(Component component, XElement xComponent)
        {
            var s = (DropdownSound) component;
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.PopupEvents, xEvents, "Popup");
            ExportAudioEvents(s.ValueChangeEvents, xEvents, "ValueChange");
            ExportAudioEvents(s.CloseEvents, xEvents, "Close");
            xComponent.Add(xEvents);
        }

        private static void EffectSoundExporter(Component component, XElement xComponent)
        {
            var s = (EffectSound) component;
            ExportSpatialSettings(s, xComponent);
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("DelayTime", s.DelayTime);            
            xComponent.Add(xSettings);
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.EnableEvents, xEvents, "Enable");
            ExportAudioEvents(s.DisableEvents, xEvents, "Disable");
            xComponent.Add(xEvents);
        }

        private static void EventSoundExporter(Component component, XElement xComponent)
        {
	        var s = (EventSound) component;
	        var xEvents = new XElement("UIAudioEvents");
	        foreach (var uiAudioEvent in s.AudioEvents)
	        {
		        ExportEvent(uiAudioEvent.AudioEvent, xEvents, uiAudioEvent.TriggerType.ToString());
	        }
	        xComponent.Add(xEvents);
        }
        
        private static void EmitterSoundExporter(Component component, XElement xComponent)
        {
            var s = (EmitterSound) component;
            var xSettings = new XElement("Settings");
            xSettings.SetAttributeValue("PauseIfInvisible", s.PauseIfInvisible);
            xComponent.Add(xSettings);
            ExportSpatialSettings(s, xComponent);
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.AudioEvents, xEvents);                        
            xComponent.Add(xEvents);                                                  
        }
        
        private static void LegacyAnimationSoundExporter(Component component, XElement xComponent)
        {
	        var s = (LegacyAnimationSound) component;
	        ExportSpatialSettings(s, xComponent);
	        var xEvents = new XElement("AnimationAudioEvents");
	        foreach (var animationAudioEvent in s.AudioEvents)
	        {
		        var xEvent = new XElement("AnimationAudioEvent");
		        xEvent.SetAttributeValue("ClipName", animationAudioEvent.ClipName);
		        xEvent.SetAttributeValue("Frame", animationAudioEvent.Frame);
		        ExportEvent(animationAudioEvent.AudioEvent, xEvent);
		        xEvents.Add(xEvent);
	        }
	        xComponent.Add(xEvents);
        }
                        
        private static void LoadBankExporter(Component component, XElement xComponent)
        {
            var s = (LoadBank) component;    
            var xSettings = new XElement("Settings");      
            xSettings.SetAttributeValue("AsyncMode", s.AsyncMode);
            xComponent.Add(xSettings); 
            ExportSpatialSettings(s, xComponent);
            ExportTriggerSettings(s, xComponent);
            ExportAsyncBankRefs(s.AsyncBanks, xComponent);
            ExportSyncBanks(s.SyncBanks, xComponent);
        }    
        
        private static void MenuSoundExporter(Component component, XElement xComponent)
        {
            var s = (MenuSound) component;                   
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.OpenEvents, xEvents, "Open");
            ExportAudioEvents(s.CloseEvents, xEvents, "Close");
            xComponent.Add(xEvents);                                                          
        }

        private static void SetSwitchExporter(Component component, XElement xComponent)
        {
            var s = (SetSwitch) component;                    
            ExportTriggerSettings(s, xComponent);
            var xSettings = new XElement("Settings");      
            xSettings.SetAttributeValue("IsGlobal", s.IsGlobal);
            xComponent.Add(xSettings);     
            var xSwitches = new XElement("Switches");
            ExportSwitches(s.OnSwitches, xSwitches, "On");
            ExportSwitches(s.OffSwitches, xSwitches, "Off");            
            xComponent.Add(xSwitches);            
        }           
        
        private static void TimelineSoundExporter(Component component, XElement xComponent)
        {
	        var s = (TimelineSound) component;
	        ExportSpatialSettings(s, xComponent);
	        var xEmitters = new XElement("Emitters");
	        ExportGameObjects(s.gameObject, s.Emitters, xEmitters);
	        xComponent.Add(xEmitters);
        }
        
        private static void ToggleSoundExporter(Component component, XElement xComponent)
        {
            var s = (ToggleSound) component;                      
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.ToggleOnEvents, xEvents, "ToggleOn");
            ExportAudioEvents(s.ToggleOffEvents, xEvents, "ToggleOff");
            xComponent.Add(xEvents);           
        }

        private static void ScrollSoundExporter(Component component, XElement xComponent)
        {
            var s = (ScrollSound) component;                                           
            ExportEvent(s.ScrollEvent, xComponent);            
        }

        private static void SliderSoundExporter(Component component, XElement xComponent)
        {
            var s = (SliderSound) component;                
            ExportParameter(s.ConnectedParameter, xComponent);
            ExportEvent(s.DragEvent, xComponent);                                                                                                                               
        }       
        
        public static void AudioStateExporter(AudioState s, XElement xComponent)
        {
            var xSettings = new XElement("Settings");			
            xSettings.SetAttributeValue("AudioState", s.AnimationAudioState.ToString());						            
            xSettings.SetAttributeValue("ResetStateOnExit", s.ResetStateOnExit);
            xComponent.Add(xSettings);
            
            var xEvents = new XElement("AudioEvents");
            ExportAudioEvents(s.EnterEvents, xEvents, "Enter");
            ExportAudioEvents(s.ExitEvents, xEvents, "Exit");
            xComponent.Add(xEvents);
            
            var xSwitches = new XElement("Switches");
            ExportSwitches(s.EnterSwitches, xSwitches, "Enter");
            ExportSwitches(s.ExitSwitches, xSwitches, "Exit"); 
            xComponent.Add(xSwitches);
        }

        public static void AudioTimelineClipExporter(AudioTimelineClip component, TimelineClip clip, XElement xComponent)
        {
	        var xSettings = new XElement("Settings");
	        xSettings.SetAttributeValue("StartTime", clip.start.ToString("0.00"));
	        xSettings.SetAttributeValue("Duration", clip.duration.ToString("0.00"));
	        xSettings.SetAttributeValue("EmitterIndex", component.EmitterIndex);
	        xSettings.SetAttributeValue("GlobalSwitch", component.GlobalSwitch);
	        xComponent.Add(xSettings);
	        var xEvents = new XElement("AudioEvents");
	        ExportAudioEvents(component.StartEvents, xEvents, "Start");
	        ExportAudioEvents(component.EndEvents, xEvents, "End");
	        xComponent.Add(xEvents);
	        var xSwitches = new XElement("Switches");
	        ExportSwitches(component.StartSwitches, xSwitches, "Start");
	        ExportSwitches(component.EndSwitches, xSwitches, "End");
	        xComponent.Add(xSwitches);
        }
        #endregion 
        
        #region Importers                
        private static bool AudioTagImporter(Component component, XElement xComponent)
        {
            var s = (AudioTag) component;    
            var xSettings = xComponent.Element("Settings");
            return ImportEnum(ref s.Tags, AsScriptingHelper.GetXmlAttribute(xSettings, "Tags"));            
        }

        private static bool AudioListener3DImporter(Component component, XElement xComponent)
        {
	        var s = (AudioListener3D) component;
	        var xSettings = xComponent.Element("Settings");
	        var modified = ImportVector3(ref s.PositionOffset, AsScriptingHelper.GetXmlAttribute(xSettings, "PositionOffset"));
	        modified |= ImportBool(ref s.MoveZAxisByCameraFOV, AsScriptingHelper.GetXmlAttribute(xSettings, "MoveZAxisByCameraFOV"));
	        modified |= ImportFloat(ref s.MinFOV, AsScriptingHelper.GetXmlAttribute(xSettings, "MinFOV"));
	        modified |= ImportFloat(ref s.MaxFOV, AsScriptingHelper.GetXmlAttribute(xSettings, "MaxFOV"));
	        modified |= ImportFloat(ref s.MinOffset, AsScriptingHelper.GetXmlAttribute(xSettings, "MinOffset"));
	        modified |= ImportFloat(ref s.MaxOffset, AsScriptingHelper.GetXmlAttribute(xSettings, "MaxOffset"));
	        return modified;
        }
        
        private static bool ButtonSoundImporter(Component component, XElement xComponent)
        {
            var s = (ButtonSound) component;
            var modified = ImportEvents(ref s.ClickEvents, xComponent, "Click");
            modified |= ImportEvent(ref s.PointerEnterEvent, xComponent, "PointerEnter");
            modified |= ImportEvent(ref s.PointerExitEvent, xComponent, "PointerExit");
            return modified;
        }

        private static bool ColliderSoundImporter(Component component, XElement xComponent)
        {
            var s = (ColliderSound) component;
            var modified = ImportTriggerSettings(s, xComponent);
            modified |= ImportSpatialSettings(s, xComponent);
            modified |= ImportParameter(ref s.CollisionForceParameter, out s.ValueScale, xComponent);
            modified |= ImportEvents(ref s.EnterEvents, xComponent, "Enter");
            modified |= ImportEvents(ref s.ExitEvents, xComponent, "Exit");
            return modified;
        }
        
        private static bool DropdownSoundImporter(Component component, XElement xComponent)
        {            
            var s = (DropdownSound) component;
            var modified = ImportEvents(ref s.ValueChangeEvents, xComponent, "ValueChange");
            modified |= ImportEvents(ref s.PopupEvents, xComponent, "Popup");
            modified |= ImportEvents(ref s.CloseEvents, xComponent, "Close");
            return modified;
        }
    
        private static bool EffectSoundImporter(Component component, XElement xComponent)
        {
            var s = (EffectSound) component;
            var xSettings = xComponent.Element("Settings");
            var modified = ImportEvents(ref s.EnableEvents, xComponent, "Enable");
            modified |= ImportEvents(ref s.DisableEvents, xComponent, "Disable");
            modified |= ImportSpatialSettings(s, xComponent);
            modified |= ImportFloat(ref s.DelayTime, AsScriptingHelper.GetXmlAttribute(xSettings, "DelayTime"));
            return modified;
        }
        
        private static bool EventSoundImporter(Component component, XElement xComponent)
        {
	        var s = (EventSound) component;
	        var xEvents = xComponent.Element("UIAudioEvents");
	        if (xEvents == null) return false;
	        var newEvents = new List<UIAudioEvent>();
	        foreach (var xEvent in xEvents.Elements())
	        {
		        var audioEvent = new PostEventReference();
		        ImportEvent(ref audioEvent, xEvent, "");
		        var uiAudioEvent = new UIAudioEvent {AudioEvent = audioEvent};
		        ImportEnum(ref uiAudioEvent.TriggerType, AsScriptingHelper.GetXmlAttribute(xEvent, "Trigger"));
		        newEvents.Add(uiAudioEvent);
	        }
	        if (!newEvents.SequenceEqual(s.AudioEvents))
	        {
		        s.AudioEvents = newEvents.ToArray();
		        return true;
	        }
	        return false;
        }

        private static bool EmitterSoundImporter(Component component, XElement xComponent)
        {
            var s = (EmitterSound) component;
            var xSettings = xComponent.Element("Settings");
            var modified = ImportEvents(ref s.AudioEvents, xComponent);
            modified |= ImportSpatialSettings(s, xComponent);
            modified |= ImportBool(ref s.PauseIfInvisible, AsScriptingHelper.GetXmlAttribute(xSettings, "PauseIfInvisible"));
            return modified;
        }

        private static bool LegacyAnimationSoundImporter(Component component, XElement xComponent)
        {
	        var s = (LegacyAnimationSound) component;
	        var modified = ImportSpatialSettings(s, xComponent);
	        var xEvents = xComponent.Element("AnimationAudioEvents");
	        if (xEvents == null) return false;
	        var newEvents = new List<AnimationAudioEvent>();
	        foreach (var xEvent in xEvents.Elements())
	        {
		        var audioEvent = new PostEventReference();
		        ImportEvent(ref audioEvent, xEvent);
		        var animationAudioEvent = new AnimationAudioEvent
		        {
			        AudioEvent = audioEvent, 
			        ClipName = AsScriptingHelper.GetXmlAttribute(xEvent, "ClipName")
		        };
		        ImportInt(ref animationAudioEvent.Frame, AsScriptingHelper.GetXmlAttribute(xEvent, "Frame"));
		        newEvents.Add(animationAudioEvent);
	        }
	        if (!newEvents.SequenceEqual(s.AudioEvents))
	        {
		        s.AudioEvents = newEvents.ToArray();
		        return true;
	        }
	        return modified;
        }
                        
        private static bool LoadBankImporter(Component component, XElement xComponent)
        {
            var s = (LoadBank) component;
            var xSettings = xComponent.Element("Settings");              
            var modified = ImportBool(ref s.AsyncMode, AsScriptingHelper.GetXmlAttribute(xSettings, "AsyncMode"));
            modified |= ImportSpatialSettings(s, xComponent);
            modified |= ImportTriggerSettings(s, xComponent);
            modified |= ImportAsyncBankRefs(ref s.AsyncBanks, xComponent);
            modified |= ImportSyncBanks(ref s.SyncBanks, xComponent);
            return modified;
        }    
        
        private static bool MenuSoundImporter(Component component, XElement xComponent)
        {
            var s = (MenuSound) component;                   
            return ImportEvents(ref s.OpenEvents, xComponent, "Open") ||            
                   ImportEvents(ref s.CloseEvents, xComponent, "Close");            
        }

        private static bool SetSwitchImporter(Component component, XElement xComponent)
        {
            var s = (SetSwitch) component;                    
            var modified = ImportTriggerSettings(s, xComponent);
            var xSettings = xComponent.Element("Settings");              
            modified |= ImportBool(ref s.IsGlobal, AsScriptingHelper.GetXmlAttribute(xSettings, "IsGlobal"));
            modified |= ImportSwitches(ref s.OnSwitches, xComponent, "On");
            modified |= ImportSwitches(ref s.OffSwitches, xComponent, "Off");                           
            return modified;           
        }                            
        
        private static bool TimelineSoundImporter(Component component, XElement xComponent)
        {
	        var s = (TimelineSound) component;
	        var modified = ImportSpatialSettings(s, xComponent);
	        var xEmitters = xComponent.Element("Emitters");
	        modified |= ImportGameObjects(s.gameObject, ref s.Emitters, xEmitters);
	        return modified;
        }
        
        private static bool ToggleSoundImporter(Component component, XElement xComponent)
        {
            var s = (ToggleSound) component;
            var modified = ImportEvents(ref s.ToggleOnEvents, xComponent, "ToggleOn");
            modified |= ImportEvents(ref s.ToggleOffEvents, xComponent, "ToggleOff");
            return modified;
        }

        private static bool ScrollSoundImporter(Component component, XElement xComponent)
        {
            var s = (ScrollSound) component;                                           
            return ImportEvent(ref s.ScrollEvent, xComponent);            
        }

        private static bool SliderSoundImporter(Component component, XElement xComponent)
        {
            var s = (SliderSound) component;
            var modified = ImportParameter(ref s.ConnectedParameter, out s.ValueScale, xComponent);
            modified |= ImportEvent(ref s.DragEvent, xComponent);
            return modified;
        }     
        
        public static bool AudioStateImporter(AudioState audioState, XElement xComponent)
        {
            var xSettings = xComponent.Element("Settings");
            var modified = ImportEnum(ref audioState.AnimationAudioState, AsScriptingHelper.GetXmlAttribute(xSettings, "AudioState"));						            
            modified |= ImportBool(ref audioState.ResetStateOnExit, AsScriptingHelper.GetXmlAttribute(xSettings, "ResetStateOnExit"));
            modified |= ImportEvents(ref audioState.EnterEvents, xComponent, "Enter");
            modified |= ImportEvents(ref audioState.ExitEvents, xComponent, "Exit");
            modified |= ImportSwitches(ref audioState.EnterSwitches, xComponent, "Enter");
            modified |= ImportSwitches(ref audioState.ExitSwitches, xComponent, "Exit");
            return modified;
        }
        
        public static bool AudioTimelineClipImporter(AudioTimelineClip component, TimelineClip clip, XElement xComponent)
        {            
                     
	        var xSettings = xComponent.Element("Settings");
	        var clipName = AsScriptingHelper.GetXmlAttribute(xComponent, "ClipName");
	        var modified = ImportInt(ref component.EmitterIndex, AsScriptingHelper.GetXmlAttribute(xSettings, "EmitterIndex"));
	        modified |= ImportBool(ref component.GlobalSwitch, AsScriptingHelper.GetXmlAttribute(xSettings, "GlobalSwitch"));
	        var start = AsScriptingHelper.StringToFloat(AsScriptingHelper.GetXmlAttribute(xSettings, "StartTime"));
	        if (clip.displayName != clipName)
	        {
		        clip.displayName = clipName;
		        modified = true;
	        }
	        if (Math.Abs(clip.start - start) >= 0.01f)
	        {
		        clip.start = start;
		        modified = true;
	        }
	        var duration = AsScriptingHelper.StringToFloat(AsScriptingHelper.GetXmlAttribute(xSettings, "Duration"));
	        if (Math.Abs(clip.duration - duration) >= 0.01f)
	        {
		        clip.duration = duration;
		        modified = true;
	        }
	        modified |= ImportEvents(ref component.StartEvents, xComponent, "Start");
	        modified |= ImportEvents(ref component.EndEvents, xComponent, "End");   
	        modified |= ImportSwitches(ref component.StartSwitches, xComponent, "Start");
	        modified |= ImportSwitches(ref component.EndSwitches, xComponent, "End");   
	        return modified;
        }
        #endregion
        
        #region Refreshers
        public static void RefreshEvent(PostEventReference evt)
        {
	        var assets = AssetDatabase.FindAssets(evt.Name);
	        if (!assets.Contains(evt.Name))
		        evt = new PostEventReference();
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