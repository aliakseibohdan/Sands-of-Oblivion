using UnityEngine;
using System.Collections.Generic;

public class SceneManager : MonoBehaviour
{
    [Header("Initialization Settings")]
    [SerializeField] private string _defaultSubscene = "Port Sudan";
    [SerializeField] private bool _initializeOnStart = true;

    [Header("Subscenes")]
    [SerializeField] private List<Subscene> _allSubscenes = new();

    private readonly Dictionary<string, Subscene> _subsceneDict = new();
    private Subscene _currentSubscene;

    private void Awake()
    {
        InitializeSubsceneDictionary();

        // Set initial background to black
        if (BackgroundManager.Instance != null)
        {
            BackgroundManager.Instance.SetBackground(null, true);
        }
    }

    private void Start()
    {
        if (_initializeOnStart)
        {
            InitializeDefaultSubscene();
        }
    }

    private void InitializeSubsceneDictionary()
    {
        _subsceneDict.Clear();
        foreach (var scene in _allSubscenes)
        {
            if (!string.IsNullOrEmpty(scene.SceneName)
                && !_subsceneDict.ContainsKey(scene.SceneName))
            {
                _subsceneDict.Add(scene.SceneName, scene);
                scene.SetActive(false); // Deactivate all initially
            }
        }
    }

    public void InitializeDefaultSubscene()
    {
        if (!string.IsNullOrEmpty(_defaultSubscene))
        {
            SwitchSubScene(_defaultSubscene);
        }
        else if (_allSubscenes.Count > 0)
        {
            SwitchSubScene(_allSubscenes[0].SceneName);
        }
    }

    public void SwitchSubScene(string sceneName)
    {
        if (_currentSubscene != null && _currentSubscene.IsActive)
        {
            StopSceneAudio(_currentSubscene);
            _currentSubscene.SetActive(false);
        }

        if (_subsceneDict.TryGetValue(sceneName, out var nextScene))
        {
            _currentSubscene = nextScene;
            nextScene.SetActive(true);

            // Update systems
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BackpackSystem.CanAccessBackpack =
                    nextScene.AllowBackpackAccess;
            }
        }
        else
        {
            Debug.LogError($"Subscene {sceneName} not found!");
        }
    }

    public Subscene GetCurrentSubscene() => _currentSubscene;

    private void StopSceneAudio(Subscene scene)
    {
        if (SoundManager.Instance == null) return;

        foreach (var sfx in scene.SoundEffects)
        {
            SoundManager.Instance.StopLoopingSFX($"Scene_{scene.SceneName}_{sfx.name}");
        }
    }

#if UNITY_EDITOR
    public void RefreshSubsceneList()
    {
        _allSubscenes.Clear();
        var sceneObjects = FindObjectsByType<SubsceneRoot>(FindObjectsSortMode.None);
        foreach (var obj in sceneObjects)
        {
            _allSubscenes.Add(obj.GetSubscene());
        }
    }
#endif
}