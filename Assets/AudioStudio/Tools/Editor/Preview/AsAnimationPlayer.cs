using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioStudio.Configs;
using AudioStudio.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AudioStudio.Tools
{
    public class AsAnimationPlayer : EditorWindow
    {
        private Animator _animator;
        private AnimatorController _tempAnimator;
        private string _currentPlaying;
        private Dictionary<AnimatorControllerLayer, ChildAnimatorState[]> _layers;
        private GameObject _model;
        private Vector2 _scrollPosition;

        private SerializedObject _serializedObject;
        public AnimatorController Animator;
        public List<AnimationClip> Clips = new List<AnimationClip>();
        public GameObject ModelPrefab;
        public List<SoundBankReference> SoundBanks;

        private void Start()
        {
            if (!ModelPrefab)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a model prefab!", "OK");
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            EditorApplication.isPlaying = true;
            EditorUtility.DisplayProgressBar("Please Wait", "Launching Game...", 1);
        }

        private void Init()
        {
            AudioInitSettings.Instance.InitializeWithoutLoading();
            foreach (var bank in SoundBanks)
            {
                bank.Load(); 
            }
            _model = Instantiate(ModelPrefab);
            Camera.main.fieldOfView = 30;
            Reset();
            EditorUtility.ClearProgressBar();
        }

        private void Exit()
        {
            EditorApplication.isPlaying = false;
            if (_model)
                DestroyImmediate(_model);
            _animator = null;
            _currentPlaying = null;
            _layers = null;
        }
        
        private void Reset()
        {
            _layers = new Dictionary<AnimatorControllerLayer, ChildAnimatorState[]>();
            _animator = _model.GetComponentInChildren<Animator>();
            var controller = _animator.runtimeAnimatorController as AnimatorController;
            if (!controller)
                _animator.runtimeAnimatorController = controller = Animator;
            else
                Animator = controller;

            if (controller)
            {
                foreach (var layer in controller.layers)
                {
                    _layers[layer] = layer.stateMachine.states;
                }
            }
            
            if (Clips.Count > 0)
            {
                _tempAnimator = new AnimatorController();
                _tempAnimator.AddLayer("Base Layer");
                foreach (var clip in Clips)
                {
                    if (!clip) continue;
                    var state = _tempAnimator.layers[0].stateMachine.AddState(clip.name);
                    state.motion = clip;
                }
            }
        }

        private void Save()
        {
            if (!ModelPrefab)
            {
                EditorUtility.DisplayDialog("Can't Save", "Model prefab is not assigned!", "OK");
                return;
            }
            var filePath = EditorUtility.SaveFilePanel("Select config", AsPathSettings.EditorConfigPathFull, ModelPrefab.name + ".json", "json");
            if (string.IsNullOrEmpty(filePath)) return;
            var obj = new
            {
                gameObject = AssetDatabase.GetAssetPath(ModelPrefab),
                animator = AssetDatabase.GetAssetPath(Animator),
                clips = Clips.Select(AssetDatabase.GetAssetPath).ToArray(),
                soundBank = SoundBanks.Select(b => b.Name).ToArray()
            };
            File.WriteAllText(filePath, AsScriptingHelper.ToJson(obj));
        }

        private void Load()
        {
            var filePath = EditorUtility.OpenFilePanel("Select config", AsPathSettings.EditorConfigPathFull, "json");
            if (string.IsNullOrEmpty(filePath)) return;
            var jsonString = File.ReadAllText(filePath);
            ModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AsScriptingHelper.FromJson(jsonString, "gameObject"));
            Animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(AsScriptingHelper.FromJson(jsonString, "animator"));
            
            Clips.Clear();
            var clipPathArray = AsScriptingHelper.FromJson(jsonString, "clips");
            foreach (var path in AsScriptingHelper.ParseJsonArray(clipPathArray))
            {
                Clips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(path));
            }
            
            SoundBanks.Clear();
            var bankNameArray = AsScriptingHelper.FromJson(jsonString, "soundBank");
            foreach (var bankName in AsScriptingHelper.ParseJsonArray(bankNameArray))
            {
                SoundBanks.Add(new SoundBankReference(bankName));
            }
        }

        #region GUI
        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
        }
        
        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                _serializedObject.Update();
                
                EditorGUILayout.LabelField("Select Model Prefab:", EditorStyles.boldLabel);
                ModelPrefab = EditorGUILayout.ObjectField(ModelPrefab, typeof(GameObject), false) as GameObject;
                EditorGUILayout.LabelField("Select Animator Controller:", EditorStyles.boldLabel);
                Animator = EditorGUILayout.ObjectField(Animator, typeof(AnimatorController), false) as AnimatorController;
                AsGuiDrawer.DrawList(Clips, "Custom Animation Clips:");
                AsGuiDrawer.DrawList(_serializedObject.FindProperty("SoundBanks"), "SoundBanks To Load:");
                _serializedObject.ApplyModifiedProperties();
                
                EditorGUILayout.BeginHorizontal();
                GUI.contentColor = Color.green;
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                    Save();
                
                GUI.contentColor = Color.magenta;
                if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                    Load();
                GUI.contentColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start"))
                EditorApplication.delayCall += Start;
            if (GUILayout.Button("Exit"))
                EditorApplication.delayCall += Exit;
            if (GUILayout.Button("Reset"))
                EditorApplication.delayCall += Reset;
            EditorGUILayout.EndHorizontal();

            if (!EditorApplication.isPlaying) return;
            if (_layers == null)
                Init();
            DrawTransport();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Play Animation:", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                foreach (var layer in _layers)
                {
                    EditorGUILayout.LabelField(layer.Key.name + ":");
                    foreach (var state in layer.Value)
                    {
                        var stateName = state.state.name;
                        if (GUILayout.Button(stateName))
                        {
                            _currentPlaying = stateName;
                            _animator.runtimeAnimatorController = Animator;
                            Play();
                        }
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Single Clips:");
                foreach (var clip in Clips)
                {
                    if (!clip) continue;
                    if (GUILayout.Button(clip.name))
                    {
                        _currentPlaying = clip.name;
                        _animator.runtimeAnimatorController = _tempAnimator;
                        Play();
                    }
                }
            }
            GUILayout.EndScrollView();
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
        #endregion
        
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
            _currentPlaying = string.Empty;
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