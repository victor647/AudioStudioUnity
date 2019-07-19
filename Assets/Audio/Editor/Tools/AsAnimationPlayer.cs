using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AudioStudio{
    public class AsAnimationPlayer : EditorWindow
    {
        public GameObject ModelPrefab;                
        public AnimatorController Animator;
        public SoundBankReference SoundBank;

        private SerializedObject _serializedObject;
        private GameObject _model;
        private Animator _animator;           
        private string _currentPlaying;
        private Dictionary<AnimatorControllerLayer, ChildAnimatorState[]> _layers;

        private void Create()
        {
            if (!Application.isPlaying)
                return;
            AudioInitSettings.Instance.InitializeWithoutObjects();
            SoundBank.Load();
            _layers = new Dictionary<AnimatorControllerLayer, ChildAnimatorState[]>();
            _model = Instantiate(ModelPrefab);
            _animator = _model.GetComponentInChildren<Animator>();            
            var controller = _animator.runtimeAnimatorController as AnimatorController;
            if (!controller)                             
                _animator.runtimeAnimatorController = controller = Animator;            

            foreach (var layer in controller.layers)
            {
                _layers[layer] = layer.stateMachine.states;
            }            
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
        }

        private void Delete()
        {
            if (_model)
                DestroyImmediate(_model);
            _animator = null;
            _currentPlaying = null;
            _layers = null;
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {                
                EditorGUILayout.LabelField("Select Model Prefab:", EditorStyles.boldLabel);
                ModelPrefab = EditorGUILayout.ObjectField(ModelPrefab, typeof(GameObject), false) as GameObject;
                EditorGUILayout.LabelField("Select Animator Controller:", EditorStyles.boldLabel);
                Animator = EditorGUILayout.ObjectField(Animator, typeof(AnimatorController), false) as AnimatorController;
                EditorGUILayout.LabelField("Load SoundBank:", EditorStyles.boldLabel);           
                
                EditorGUILayout.BeginHorizontal();                                
                EditorGUILayout.PropertyField(_serializedObject.FindProperty("SoundBank"), GUIContent.none);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create"))
                    Create();
                if (GUILayout.Button("Delete"))
                    Delete();
                EditorGUILayout.EndHorizontal();
            }

            if (_layers == null) return;
            DrawTransport();            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Play Animation:", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                foreach (var layer in _layers)
                {
                    EditorGUILayout.LabelField(layer.Key.name + ": ");
                    foreach (var state in layer.Value)
                    {
                        var stateName = state.state.name;                        
                        if (GUILayout.Button(stateName))
                        {                                                
                            _currentPlaying = stateName;
                            Play();
                        }
                    }                    
                }
            }
        }

        private void DrawTransport()
        {            
            EditorGUILayout.BeginHorizontal();
            
            GUI.contentColor = Color.green;
            if (GUILayout.Button("Play", EditorStyles.miniButtonLeft, GUILayout.Width(40)))                                    					                
                Play();    
            GUI.contentColor = Color.yellow;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause/Resume", EditorStyles.miniButtonMid, GUILayout.Width(90)))
                Pause();
            GUI.contentColor = Color.red;
            if (GUILayout.Button("Stop", EditorStyles.miniButtonRight, GUILayout.Width(40)))
                Stop();
                        
            GUI.contentColor = Color.cyan;
            if (GUILayout.Button("Last Frame", EditorStyles.miniButtonLeft, GUILayout.Width(70)))
                NudgeBack();
            if (GUILayout.Button("Next Frame", EditorStyles.miniButtonRight, GUILayout.Width(70)))
                NudgeForward();
            GUI.contentColor = Color.white;           
            EditorGUILayout.EndHorizontal();                
        }
        
        #region Controls
        private void Play()
        {            
            _animator.enabled = true;
            Resume();
            _animator.Play(_currentPlaying);
        }
        
        private void Stop()
        {
            _animator.enabled = false;
        }
        
        private void Pause()
        {            
            _animator.speed = _animator.speed == 1 ? 0 : 1;
        }

        private void Resume()
        {
            _animator.speed = 1;
        }
        
        private void NudgeForward()
        {   
            Resume();
            _animator.Update(Time.fixedDeltaTime);
            Pause();
        }  
        
        private void NudgeBack()
        {            
            Resume();
            _animator.Update(Time.fixedDeltaTime * -1f);
            Pause();
        }        
        #endregion
    }
}