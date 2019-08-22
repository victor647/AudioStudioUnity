using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using UnityEditor.VersionControl;

namespace AudioStudio.Tools
{
    public static class AudioUtility
    {
        #region XML
        public static void WriteXml(string fileName, XElement xRoot)
        {
            var settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            var xmlWriter = XmlWriter.Create(fileName, settings);
            xRoot.Save(xmlWriter);
            xmlWriter.Flush();
            xmlWriter.Close();
        }

        public static string GetXmlAttribute(XElement node, string attribute)
        {
            var a = node.Attribute(attribute);
            return a != null ? a.Value : "";
        }
        
        public static string GetXmlElement(XElement node, string nodeName)
        {
            var a = node.Element(nodeName);
            return a != null ? a.Value : "";
        }
        #endregion
        
        #region TypeCast
        public static float StringToFloat(string s)
        {
            float outValue;
            float.TryParse(s, out outValue);
            return outValue;
        }

        public static byte StringToByte(string s)
        {
            byte outValue;
            byte.TryParse(s, out outValue);
            return outValue;
        }
        
        public static bool StringToBool(string s)
        {
            bool outValue;
            bool.TryParse(s, out outValue);
            return outValue;
        }
        
        public static Vector3 StringToVector3(string s)
        {
            var numbers = s.Split(',');
            if (numbers.Length != 3) return Vector3.zero;
            float x, y, z;
            float.TryParse(numbers[0].Trim(), out x);
            float.TryParse(numbers[1].Trim(), out y);
            float.TryParse(numbers[2].Trim(), out z);
            return new Vector3(x, y, z);
        }
        
        public static Type StringToType(string typeName)
        {
            if (typeName == "") return null;
            var assembly = Assembly.Load("Assembly-CSharp");
            return (assembly.GetType(typeName) ?? assembly.GetType("AudioStudio.Components." + typeName)) ??
                   Type.GetType("UnityEngine." + typeName + ", UnityEngine");
        }  
        
        public static void AddToArray<T>(ref T[] array, T element)
        {
            List<T> list = array.ToList();
            list.Add(element);
            array = list.ToArray();
        }
        
        public static void RemoveFromArray<T>(ref T[] array, T element)
        {
            List<T> list = array.ToList();
            list.Remove(element);
            array = list.ToArray();
        }
        
        public static uint GenerateUniqueID(string in_name)
        {
            var buffer = Encoding.UTF8.GetBytes(in_name.ToLower());
            var hval = 2166136261;

            for (var i = 0; i < buffer.Length; i++)
            {
                hval *= 16777619;
                hval ^= buffer[i];
            }
            return hval;
        }
        #endregion

        #region FileIO
        public static void CheckoutLockedFile(string filePath)
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
        
        public static void CheckDirectoryExist(string directory)
        {
            if (directory != null && !Directory.Exists(directory)) 
                Directory.CreateDirectory(directory);				
        }
        
        public static string ShortPath(string longPath)
        {
            longPath = longPath.Replace("\\", "/");
            var index = longPath.IndexOf("Assets", StringComparison.Ordinal);
            return index >= 0 ? longPath.Substring(index) : longPath;
        }

        public static string CombinePath(string path1, string path2, string path3 = "")
        {
            var path = Path.Combine(path1, path2);
            if (path3 != "") 
                path = Path.Combine(path, path3);
            return path.Replace("\\", "/");            
        }
        #endregion
        
        #region Process
        public static void RunCommand(string cmd, string arguments)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = cmd,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    Arguments = arguments
                }
            };

            process.Start();            
            
            var reader = process.StandardOutput;
            reader.ReadToEnd();          
            process.WaitForExit();
            process.Close();
        }
        #endregion
    }
}