using System;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;


namespace AudioStudio
{
    public static class AudioScriptGUI
    {
        public static void DrawList(SerializedProperty list, string label = "", Action<Object[]> dragDropReceiver = null)
        {
            if (label != "")
            {
                var dropArea = new EditorGUILayout.VerticalScope(GUI.skin.label);
                using (dropArea)
                {
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    if (dragDropReceiver != null)
                        CheckDragDropAction(dropArea.rect, dragDropReceiver);
                }
            }

            var size = list.FindPropertyRelative("Array.size");
            if (size.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Not showing lists with different sizes.", UnityEditor.MessageType.Info);
                return;
            }

            var box = new EditorGUILayout.VerticalScope(GUI.skin.box);
            using (box)
            {
                if (list.arraySize == 0)
                {
                    GUI.contentColor = Color.yellow;
                    if (GUILayout.Button("Create", EditorStyles.miniButton))
                        list.arraySize++;
                    GUI.contentColor = Color.white;
                    if (dragDropReceiver != null)
                        CheckDragDropAction(box.rect, dragDropReceiver);
                }
                else
                {
                    for (var i = 0; i < list.arraySize; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var index = (i + 1).ToString("00");
                        GUI.contentColor = Color.cyan;
                        EditorGUILayout.LabelField(index, GUILayout.Width(20));
                        GUI.contentColor = Color.white;
                        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
                        DrawAddDeleteButtons(list, i);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
        }

        private static void CheckDragDropAction(Rect dropArea, Action<Object[]> action)
        {
            var currentEvent = Event.current;
            if (!dropArea.Contains(currentEvent.mousePosition))
                return;

            if (currentEvent.type != EventType.DragUpdated && currentEvent.type != EventType.DragPerform)
                return;

            var reference = DragAndDrop.objectReferences;                                  

            DragAndDrop.visualMode = reference.Length > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            
            if (currentEvent.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                if (reference.Length > 0)
                    action(reference);
                DragAndDrop.PrepareStartDrag();
                GUIUtility.hotControl = 0;
            }
            currentEvent.Use();
        }

        public static void DrawAddDeleteButtons(SerializedProperty list, int index)
        {
            if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(20)))
                list.InsertArrayElementAtIndex(index);
            if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20)))
                list.DeleteArrayElementAtIndex(index);
        }
        
        public static void DrawSaveButton(Object obj)
        {
            GUI.contentColor = Color.green;
            if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
            {                
                EditorUtility.SetDirty(obj);                
                AssetDatabase.SaveAssets();
            }
            GUI.contentColor = Color.white;
        }     
        
        public static void CheckLinkedComponent<T>(Component component)
        {
            if (component.GetComponent<T>() == null)
            { 
                EditorGUILayout.HelpBox("Can't Find " + typeof(T).Name + " Component!", UnityEditor.MessageType.Error);
            }
        }
        
        public static void DisplaySearchPath(ref string searchPath)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search in", GUILayout.Width(70));
            EditorGUILayout.LabelField(AudioUtility.ShortPath(searchPath), GUI.skin.textField, GUILayout.MaxWidth(350));
            if (GUILayout.Button("Browse", EditorStyles.miniButtonRight, GUILayout.Width(60)))			
                searchPath = EditorUtility.OpenFolderPanel("Root Folder", searchPath, "");
            GUILayout.EndHorizontal();
        }
    }
}