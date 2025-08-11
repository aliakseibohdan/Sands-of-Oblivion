using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Audio;

public class MainMenuEvents : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem[] _buttonEffects;

    [Header("Audio Settings")]
    private AudioSource _audioSource;
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Scene Management")]
    [SerializeField] private SceneField _sceneToLoadFromNewGame;

    // Containers
    private UIDocument _document;
    private readonly Dictionary<string, VisualElement> _containers = new();

    // Main menu buttons
    private readonly Dictionary<MenuButton, Button> _buttons = new();
    private AudioSource[] _buttonEffectAudio;

    // Settings UI elements
    private DropdownField _qualityDropdown;
    private DropdownField _resolutionDropdown;
    private Toggle _fullscreenToggle;
    private Toggle _borderlessToggle;
    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Slider _uiVolumeSlider;
    private Slider _ambienceVolumeSlider;
    private Slider _dialogueVolumeSlider;
    private Button _settingsBackButton;

    // Resolution data
    private Resolution[] _availableResolutions;
    private int _currentResolutionIndex;

    private enum MenuButton
    {
        NewGame,
        ContinueGame,
        Settings,
        Exit,
        Back
    }

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        CacheContainers();
        CacheMainMenuButtons();
        CacheSettingsElements();
    }

    private void Start()
    {
        InitializeAudio();
        InitializeResolutionSettings();
        InitializeQualitySettings();
        InitializeVolumeSettings();
        CacheEffectAudioSources();
    }

    private void OnEnable()
    {
        RegisterMainMenuCallbacks();
        RegisterSettingsCallbacks();
        ShowContainer("MainMenuContainer", true, true);
    }

    private void OnDisable()
    {
        UnregisterMainMenuCallbacks();
        UnregisterSettingsCallbacks();
    }

    private void CacheContainers()
    {
        var containers = _document.rootVisualElement.Query<VisualElement>(className: "menu-container").ToList();
        foreach (var container in containers)
        {
            _containers[container.name] = container;

            // Hide all containers by default
            container.style.display = DisplayStyle.None;
            container.style.opacity = 0;
        }
    }

    private void CacheMainMenuButtons()
    {
        _buttons[MenuButton.NewGame] = _document.rootVisualElement.Q<Button>("NewGameButton");
        _buttons[MenuButton.ContinueGame] = _document.rootVisualElement.Q<Button>("ContinueGameButton");
        _buttons[MenuButton.Settings] = _document.rootVisualElement.Q<Button>("SettingsButton");
        _buttons[MenuButton.Exit] = _document.rootVisualElement.Q<Button>("ExitButton");
    }

    private void CacheSettingsElements()
    {
        VisualElement settingsContainer = _document.rootVisualElement.Q("SettingsContainer");

        // Graphics settings
        _qualityDropdown = settingsContainer.Q<DropdownField>("QualityDropdown");
        _resolutionDropdown = settingsContainer.Q<DropdownField>("ResolutionDropdown");
        _fullscreenToggle = settingsContainer.Q<Toggle>("FullscreenToggle");
        _borderlessToggle = settingsContainer.Q<Toggle>("BorderlessToggle");

        // Audio settings
        _masterVolumeSlider = settingsContainer.Q<Slider>("MasterVolumeSlider");
        _musicVolumeSlider = settingsContainer.Q<Slider>("MusicVolumeSlider");
        _sfxVolumeSlider = settingsContainer.Q<Slider>("SFXVolumeSlider");
        _uiVolumeSlider = settingsContainer.Q<Slider>("UIVolumeSlider");
        _ambienceVolumeSlider = settingsContainer.Q<Slider>("AmbienceVolumeSlider");
        _dialogueVolumeSlider = settingsContainer.Q<Slider>("DialogueVolumeSlider");

        _settingsBackButton = settingsContainer.Q<Button>("SettingsBackButton");
        _buttons[MenuButton.Back] = _settingsBackButton;
    }

    private void CacheEffectAudioSources()
    {
        if (_buttonEffects == null) return;

        _buttonEffectAudio = new AudioSource[_buttonEffects.Length];
        for (int i = 0; i < _buttonEffects.Length; i++)
        {
            if (_buttonEffects[i] != null)
                _buttonEffectAudio[i] = _buttonEffects[i].GetComponent<AudioSource>();
        }
    }

    private void InitializeAudio()
    {
        if (TryGetComponent(out AudioSource source))
        {
            _audioSource = source;
        }
        else
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void InitializeResolutionSettings()
    {
        // Get available resolutions
        _availableResolutions = Screen.resolutions;
        _resolutionDropdown.choices.Clear();

        // Filter duplicates
        var uniqueResolutions = new List<Resolution>();
        foreach (var resolution in _availableResolutions)
        {
            if (uniqueResolutions.FindIndex(r =>
                r.width == resolution.width && r.height == resolution.height) == -1)
            {
                uniqueResolutions.Add(resolution);
            }
        }

        // Populate dropdown
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            _resolutionDropdown.choices.Add($"{uniqueResolutions[i].width} x {uniqueResolutions[i].height}");

            // Check if current resolution
            if (uniqueResolutions[i].width == Screen.currentResolution.width &&
                uniqueResolutions[i].height == Screen.currentResolution.height)
            {
                _currentResolutionIndex = i;
            }
        }

        _resolutionDropdown.index = _currentResolutionIndex;
        _resolutionDropdown.value = _resolutionDropdown.choices[_currentResolutionIndex];

        // Initialize fullscreen toggles
        _fullscreenToggle.value = Screen.fullScreen;
        _borderlessToggle.value = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;
        _borderlessToggle.SetEnabled(_fullscreenToggle.value);
    }

    private void InitializeQualitySettings()
    {
        // Populate quality dropdown
        _qualityDropdown.choices = new List<string>(QualitySettings.names);
        _qualityDropdown.index = QualitySettings.GetQualityLevel();
        _qualityDropdown.value = QualitySettings.names[QualitySettings.GetQualityLevel()];
    }

    private void InitializeVolumeSettings()
    {
        // Initialize with default values
        _masterVolumeSlider.value = 80;
        _musicVolumeSlider.value = 80;
        _sfxVolumeSlider.value = 80;
        _uiVolumeSlider.value = 80;
        _ambienceVolumeSlider.value = 80;
        _dialogueVolumeSlider.value = 80;

        // Set initial volume
        SetVolume("MasterVolume", _masterVolumeSlider.value);
        SetVolume("MusicVolume", _musicVolumeSlider.value);
        SetVolume("SFXVolume", _sfxVolumeSlider.value);
        SetVolume("UIVolume", _uiVolumeSlider.value);
        SetVolume("AmbienceVolume", _ambienceVolumeSlider.value);
        SetVolume("DialogueVolume", _dialogueVolumeSlider.value);
    }

    private void RegisterMainMenuCallbacks()
    {
        foreach (var kvp in _buttons)
        {
            kvp.Value.RegisterCallback<ClickEvent>(HandleMainMenuClick);
            kvp.Value.RegisterCallback<ClickEvent>(PlayButtonSound);
        }
    }

    private void UnregisterMainMenuCallbacks()
    {
        foreach (var kvp in _buttons)
        {
            kvp.Value.UnregisterCallback<ClickEvent>(HandleMainMenuClick);
            kvp.Value.UnregisterCallback<ClickEvent>(PlayButtonSound);
        }
    }

    private void RegisterSettingsCallbacks()
    {
        // Graphics settings
        _qualityDropdown.RegisterValueChangedCallback(OnQualityChanged);
        _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
        _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
        _borderlessToggle.RegisterValueChangedCallback(OnBorderlessChanged);

        // Audio settings
        _masterVolumeSlider.RegisterValueChangedCallback(evt => SetVolume("MasterVolume", evt.newValue));
        _musicVolumeSlider.RegisterValueChangedCallback(evt => SetVolume("MusicVolume", evt.newValue));
        _sfxVolumeSlider.RegisterValueChangedCallback(evt => SetVolume("SFXVolume", evt.newValue));
        _uiVolumeSlider.RegisterValueChangedCallback(evt => SetVolume("UIVolume", evt.newValue));
        _ambienceVolumeSlider.RegisterValueChangedCallback(evt => SetVolume("AmbienceVolume", evt.newValue));
        _dialogueVolumeSlider.RegisterValueChangedCallback(evt => SetVolume("DialogueVolume", evt.newValue));

        // Navigation
        _settingsBackButton.RegisterCallback<ClickEvent>(OnSettingsBackClick);
    }

    private void UnregisterSettingsCallbacks()
    {
        _qualityDropdown.UnregisterValueChangedCallback(OnQualityChanged);
        _resolutionDropdown.UnregisterValueChangedCallback(OnResolutionChanged);
        _fullscreenToggle.UnregisterValueChangedCallback(OnFullscreenChanged);
        _borderlessToggle.UnregisterValueChangedCallback(OnBorderlessChanged);

        _masterVolumeSlider.UnregisterValueChangedCallback(evt => SetVolume("MasterVolume", evt.newValue));
        // ... repeat for other sliders ...

        _settingsBackButton.UnregisterCallback<ClickEvent>(OnSettingsBackClick);
    }

    private void PlayButtonSound(ClickEvent evt)
    {
        _audioSource.Play();
    }

    private void HandleMainMenuClick(ClickEvent evt)
    {
        var button = evt.currentTarget as Button;

        foreach (var kvp in _buttons)
        {
            if (kvp.Value == button)
            {
                switch (kvp.Key)
                {
                    case MenuButton.NewGame:

                        SceneSwapManager.SwapScene(_sceneToLoadFromNewGame);
                        break;

                    case MenuButton.Settings:

                        TriggerButtonEffect(MenuButton.Settings);
                        StartCoroutine(FadeElement(_buttons[MenuButton.Back], true));
                        ShowContainer("SettingsContainer", true, true, .35f);
                        ShowContainer("MainMenuContainer", false, true, .35f);
                        break;

                    case MenuButton.Exit:

                        TriggerButtonEffect(MenuButton.Exit);
                        StartCoroutine(QuitAfterDelay(2f));
                        break;

                    default:

                        TriggerButtonEffect(kvp.Key);
                        break;
                }
                return;
            }
        }
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void TriggerButtonEffect(MenuButton buttonType)
    {
        _buttons[buttonType].visible = false;

        int index = (int)buttonType;
        if (index < _buttonEffects.Length && _buttonEffects[index] != null)
        {
            _buttonEffects[index].Play();
            if (index < _buttonEffectAudio.Length && _buttonEffectAudio[index] != null)
            {
                _buttonEffectAudio[index].Play();
            }
        }
    }

    private void OnSettingsBackClick(ClickEvent evt)
    {
        ShowAllButtons();
        TriggerButtonEffect(MenuButton.Back);
        ShowContainer("MainMenuContainer", true, true, .35f);
        ShowContainer("SettingsContainer", false, true, .35f);
    }

    #region Settings Handlers
    private void OnQualityChanged(ChangeEvent<string> evt)
    {
        QualitySettings.SetQualityLevel(_qualityDropdown.index);
    }

    private void OnResolutionChanged(ChangeEvent<string> evt)
    {
        _currentResolutionIndex = _resolutionDropdown.index;
        Resolution resolution = _availableResolutions[_currentResolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    private void OnFullscreenChanged(ChangeEvent<bool> evt)
    {
        Screen.fullScreen = evt.newValue;
        _borderlessToggle.SetEnabled(evt.newValue);

        // Reset borderless when disabling fullscreen
        if (!evt.newValue)
        {
            _borderlessToggle.value = false;
        }
    }

    private void OnBorderlessChanged(ChangeEvent<bool> evt)
    {
        Screen.fullScreenMode = evt.newValue ?
            FullScreenMode.FullScreenWindow :
            FullScreenMode.ExclusiveFullScreen;
    }

    private void SetVolume(string parameterName, float value)
    {
        if (_audioMixer == null) return;

        // Convert 0-100 slider to -80dB to 0dB
        float dB = value > 0 ?
            Mathf.Log10(value / 100) * 20 :
            -80;

        _audioMixer.SetFloat(parameterName, dB);
    }
    #endregion

    #region Container Management
    public void ShowContainer(string containerName, bool show, bool fade = true,
                             float delay = 0f, float duration = -1f)
    {
        if (!_containers.TryGetValue(containerName, out var container))
        {
            Debug.LogWarning($"Container not found: {containerName}");
            return;
        }

        if (duration < 0) duration = _fadeDuration;

        if (fade)
        {
            StartCoroutine(SetContainerVisibilityWithFade(container, show, delay, duration));
        }
        else
        {
            container.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            container.style.opacity = show ? 1f : 0f;
        }
    }

    private IEnumerator SetContainerVisibilityWithFade(VisualElement container, bool show,
                                                      float delay, float duration)
    {
        yield return new WaitForSeconds(delay);

        if (show)
        {
            container.style.display = DisplayStyle.Flex;
        }

        float startOpacity = container.style.opacity.value;
        float targetOpacity = show ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            container.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / duration);
            yield return null;
        }

        container.style.opacity = targetOpacity;

        if (!show)
        {
            container.style.display = DisplayStyle.None;
        }
    }
    #endregion

    #region Button Management
    public void ShowAllButtons()
    {
        foreach (var kvp in _buttons)
        {
            StartCoroutine(FadeElement(kvp.Value, true));
        }
    }

    private IEnumerator FadeElement(VisualElement element, bool fadeIn, float duration = -1f)
    {
        if (duration < 0) duration = _fadeDuration;

        if (fadeIn)
        {
            element.visible = true;
            element.style.opacity = 0;
        }

        float startOpacity = element.style.opacity.value;
        float targetOpacity = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            element.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / duration);
            yield return null;
        }

        element.style.opacity = targetOpacity;

        if (!fadeIn)
        {
            element.style.display = DisplayStyle.None;
        }
    }
    #endregion
}