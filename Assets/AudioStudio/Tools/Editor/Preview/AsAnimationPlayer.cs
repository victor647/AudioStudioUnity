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
        private float _speed = 1;
        private Dictionary<AnimatorControllerLayer, ChildAnimatorState[]> _layers;
        private GameObject _model;
        private Vector2 _scrollPosition;

        private SerializedObject _serializedObject;
        public AnimatorController Animator;
        public List<AnimationClip> Clips = new List<AnimationClip>();
        public GameObject ModelPrefab;
        public List<SoundBank> SoundBanks = new List<SoundBank>();
        private bool _useEmptyScene = true;
        private bool _legacyMode;
        private Animation _legacyAnimation;

        private void Start()
        {
            if (!_legacyMode && !ModelPrefab)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a model prefab!", "OK");
                return;
            }
            if (_useEmptyScene)
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            EditorApplication.isPlaying = true;
            EditorUtility.DisplayProgressBar("Please Wait", "Launching Game...", 1);
        }

        private void Init()
        {
            AudioInitSettings.Instance.Initialize(false);
            foreach (var bank in SoundBanks)
            {
                bank.Load();
            }
            _model = Instantiate(ModelPrefab);
            Reset();

            if (_useEmptyScene)
                Camera.main.fieldOfView = 30;
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
            if (_legacyMode)
            {
                _legacyAnimation = _model.GetComponentInChildren<Animation>();
                if (!_legacyAnimation) return;
                Clips = new List<AnimationClip>();
                foreach (AnimationState state in _legacyAnimation)
                {
                    Clips.Add(state.clip);
                }
            }
            else
            {
                _layers = new Dictionary<AnimatorControllerLayer, ChildAnimatorState[]>();
                if (!_model) return;
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
        }

        private void Save()
        {
            if (!ModelPrefab)
            {
                EditorUtility.DisplayDialog("Can't Save", "Model prefab is not assigned!", "OK");
                return;
            }
            var filePath = EditorUtility.SaveFilePanel("Select config", AudioPathSettings.EditorConfigPathFull, ModelPrefab.name + ".json", "json");
            if (string.IsNullOrEmpty(filePath)) return;
            var obj = new
            {
                legacy = _legacyMode,
                newScene = _useEmptyScene,
                gameObject = AssetDatabase.GetAssetPath(ModelPrefab),
                animator = AssetDatabase.GetAssetPath(Animator),
                clips = Clips.Select(AssetDatabase.GetAssetPath).ToArray(),
                soundBank = SoundBanks.Select(b => b.name).ToArray()
            };
            File.WriteAllText(filePath, AsScriptingHelper.ToJson(obj));
        }

        private void Load()
        {
            var filePath = EditorUtility.OpenFilePanel("Select config", AudioPathSettings.EditorConfigPathFull, "json");
            if (string.IsNullOrEmpty(filePath)) return;
            var jsonString = File.ReadAllText(filePath);

            _legacyMode = AsScriptingHelper.StringToBool(AsScriptingHelper.FromJson(jsonString, "legacy"));
            if (!_legacyMode)
            {
                _useEmptyScene = AsScriptingHelper.StringToBool(AsScriptingHelper.FromJson(jsonString, "newScene"));
                ModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AsScriptingHelper.FromJson(jsonString, "gameObject"));
                Animator = AssetDatabase.LoadAssetAtPath<AnimatorController>(AsScriptingHelper.FromJson(jsonString, "animator"));
            }

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
                var bankPath = AsScriptingHelper.CombinePath("Assets", AudioPathSettings.Instance.SoundBanksPath, bankName + ".asset");
                var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(bankPath);
                if (bank)
                    SoundBanks.Add(bank);
            }
        }

        #region GUI
        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
        }
        
        private void OnGUI()
        {
            _legacyMode = EditorGUILayout.Toggle("Legacy Mode", _legacyMode);
            _useEmptyScene = !_legacyMode && EditorGUILayout.Toggle("Use Empty Scene", _useEmptyScene);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                _serializedObject.Update();
                EditorGUILayout.LabelField("Select Model Prefab:", EditorStyles.boldLabel);
                ModelPrefab = EditorGUILayout.ObjectField(ModelPrefab, typeof(GameObject), false) as GameObject;
                if (!_legacyMode)
                {
                    EditorGUILayout.LabelField("Select Animator Controller:", EditorStyles.boldLabel);
                    Animator = EditorGUILayout.ObjectField(Animator, typeof(AnimatorController), false) as AnimatorController;
                    AsGuiDrawer.DrawList(Clips, "Custom Animation Clips:");
                }
                
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
            if (!_model)
                Init();
            DrawTransport();
            
            EditorGUILayout.LabelField("Play Animation:", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            if (!_legacyMode)
            {
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
            }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Single Clips:");
                foreach (var clip in Clips)
                {
                    if (!clip) continue;
                    if (GUILayout.Button(clip.name))
                    {
                        if (_legacyMode)
                            PlayLegacy(clip);
                        else
                        {
                            _currentPlaying = clip.name;
                            _animator.runtimeAnimatorController = _tempAnimator;
                            Play();
                        }
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        private void DrawTransport()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = Color.green;
            if (GUILayout.Button("Play", EditorStyles.miniButtonLeft, GUILayout.MinWidth(40)))
                Play();
            
            GUI.contentColor = Color.yellow;
            if (GUILayout.Button("Pause/Resume", EditorStyles.miniButtonMid, GUILayout.MinWidth(90)))
                Pause();

            GUI.contentColor = Color.red;
            if (GUILayout.Button("Stop", EditorStyles.miniButtonRight, GUILayout.MinWidth(40)))
                Stop();

            if (!_legacyMode)
            {
                GUI.contentColor = Color.cyan;
                if (GUILayout.Button("Last Frame", EditorStyles.miniButtonLeft, GUILayout.MinWidth(70)))
                    NudgeBack();
                if (GUILayout.Button("Next Frame", EditorStyles.miniButtonRight, GUILayout.MinWidth(70)))
                    NudgeForward();
            }

            GUI.contentColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (!_legacyMode)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Playback Speed", GUILayout.MaxWidth(100));
                var speed = _speed;
                _speed = EditorGUILayout.Slider(_speed, -2f, 2f);
                if (speed != _speed && speed != 0f)
                    UpdateAnimatorSpeed();
                EditorGUILayout.EndHorizontal();
            }
        }
        #endregion
        
        #region Controls

        private void Play()
        {
            if (_legacyMode)
                _legacyAnimation.Play();
            else
            {
                _animator.enabled = true;
                UpdateAnimatorSpeed();
                _animator.Play(_currentPlaying);
            }
        }

        private void PlayLegacy(AnimationClip clip)
        {
            _legacyAnimation.clip = clip;
            _legacyAnimation.Play();
        }

        private void Stop()
        {
            if (_legacyMode)
                _legacyAnimation.Stop();
            else
            {
                _animator.enabled = false;
                _currentPlaying = string.Empty;
            }
        }

        private void Pause()
        {
            if (_legacyMode)
                _legacyAnimation.Sample();
            else
                _animator.speed = _animator.speed == _speed ? 0 : _speed;
        }

        private void UpdateAnimatorSpeed()
        {
            _animator.speed = _speed;
        }

        private void NudgeForward()
        {
            UpdateAnimatorSpeed();
            _animator.Update(Time.fixedDeltaTime);
            Pause();
        }

        private void NudgeBack()
        {
            UpdateAnimatorSpeed();
            _animator.Update(Time.fixedDeltaTime * -1f);
            Pause();
        }

        #endregion
    }
}