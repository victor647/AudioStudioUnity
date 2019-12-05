using UnityEngine;
using System.Xml;
using System.Xml.Linq;
using System.Collections;
using UnityEditor.VersionControl;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AudioStudio.Tools
{
    public static class AsScriptingHelper
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

        public static void RemoveComponentXml(XElement xComponent)
        {
            var parent = xComponent.Parent;
            xComponent.Remove();
            if (parent != null && !parent.HasElements)
                parent.Remove();
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

        public static int StringToInt(string s)
        {
            int outValue;
            int.TryParse(s, out outValue);
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
            var list = array.ToList();
            list.Add(element);
            array = list.ToArray();
        }

        public static void RemoveFromArray<T>(ref T[] array, T element)
        {
            var list = array.ToList();
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

        public static string CombinePath(string path1, string path2, string path3 = "", string path4 = "")
        {
            var path = Path.Combine(path1, path2);
            if (path3 != "")
                path = Path.Combine(path, path3);
            if (path4 != "")
                path = Path.Combine(path, path4);
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

        #region Json

        public static string FromJson(string jsonString, string fieldName, bool trimQuote = false)
        {
            var propertyString = "\"" + fieldName + "\":";
            var indexStart = jsonString.IndexOf(propertyString, StringComparison.Ordinal);
            if (indexStart < 0)
                return "";
            indexStart += propertyString.Length;
            var trimmedValue = jsonString.Substring(indexStart);
            
            var length = 0;
            if (trimmedValue.StartsWith("["))
                length = GetJsonObjectEndIndex(trimmedValue, true);
            else if (trimmedValue.StartsWith("{"))
                length = GetJsonObjectEndIndex(trimmedValue, false);
            else
            {
                length = trimmedValue.IndexOf(',');
                if (length < 0) 
                    length = trimmedValue.IndexOf('}');
            }
                
            var value = jsonString.Substring(indexStart, length).Trim();
            return trimQuote ? value.Trim('\"') : value;
        }

        private static int GetJsonObjectEndIndex(string trimmedValue, bool isList)
        {
            var startChar = isList ? '[' : '{';
            var endChar = isList ? ']' : '}';
            var indexStart = trimmedValue.IndexOf(startChar) + 1;
            var indexEnd = trimmedValue.IndexOf(endChar) + 1;
            var between = trimmedValue.Substring(indexStart, indexEnd - indexStart);
            while (between.IndexOf(startChar) > -1)
            {
                indexStart = trimmedValue.IndexOf(startChar, indexStart) + 1;
                indexEnd = trimmedValue.IndexOf(endChar, indexEnd) + 1;
                between = trimmedValue.Substring(indexStart, indexEnd - indexStart);
            }
            return indexEnd;
        }

        public static string ToJson(object obj)
        {
            var argument = "{";
            var propertyCount = obj.GetType().GetProperties().Length;
            for (var i = 0; i < propertyCount; i++)
            {
                WriteJsonLine(ref argument, obj.GetType().GetProperties()[i], obj);
                if (i < propertyCount - 1)
                    argument += ", ";
            }
            argument += "}";
            return argument;
        }

        public static string[] ParseJsonArray(string arrayData)
        {
            var clipPaths = arrayData.Substring(1, arrayData.Length - 2).Split(',');
            return clipPaths.Select(p => p.Trim().Trim('\"')).ToArray();
        }

        private static void WriteJsonLine(ref string argument, PropertyInfo property, object obj)
        {
            var type = property.PropertyType;
            var value = property.GetValue(obj, null);
            if (type.IsArray)
            {
                argument += string.Format("\"{0}\": [", property.Name);
                var array = value as IEnumerable;
                var hasElements = false;
                if (array != null)
                {
                    foreach (var element in array)
                    {
                        hasElements = true;
                        WriteJsonArray(ref argument, element);
                        argument.Remove(argument.Length - 3);
                    }
                }

                if (hasElements)
                    argument = argument.Remove(argument.Length - 2) + "]";
                else
                    argument += "]";
            }
            else if (type == typeof(string) || type == typeof(bool))
                argument += string.Format("\"{0}\": \"{1}\"", property.Name, value);
            else if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
                argument += string.Format("\"{0}\": {1}", property.Name, value);
            else if (type == typeof(Guid))
                argument += string.Format("\"{0}\": ", property.Name) + "\"{" + value + "}\"";
            else
                argument += string.Format("\"{0}\": {1}", property.Name, ToJson(value));
        }

        private static void WriteJsonArray(ref string argument, object obj)
        {
            var type = obj.GetType();
            if (type == typeof(string) || type == typeof(bool))
                argument += string.Format("\"{0}\"", obj);
            else if (type == typeof(Guid))
                argument += "\"{" + obj + "}\"";
            else
                argument += obj.ToString();
            argument += ", ";
        }
        #endregion
    }
}