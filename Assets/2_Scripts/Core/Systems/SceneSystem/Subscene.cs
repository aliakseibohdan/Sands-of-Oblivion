using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Subscene
{
    [Header("Core Identity")]
    public string SceneName;

    [Header("Visual Elements")]
    public GameObject RootObject;
    public Sprite Background;
    public List<InteractiveElement> InteractiveElements = new();

    [Header("Audio Configuration")]
    public AudioClip Soundtrack;
    public List<AudioClip> SoundEffects = new();

    [Header("Gameplay Restrictions")]
    public bool AllowBackpackAccess = true;

    // Runtime state
    private bool _isActive;

    public void SetActive(bool active)
    {
        _isActive = active;

        if (RootObject) RootObject.SetActive(active);

        foreach (var element in InteractiveElements)
            if (element) element.gameObject.SetActive(active);

        if (active)
        {
            // Set background
            BackgroundManager.Instance.SetBackground(Background);

            // Play audio
            PlaySceneAudio();
        }
    }

    private void PlaySceneAudio()
    {
        if (SoundManager.Instance == null) return;

        // Play soundtrack
        SoundManager.Instance.PlayMusic(Soundtrack);

        // Play ambient SFX
        foreach (var sfx in SoundEffects)
        {
            SoundManager.Instance.PlayLoopingSFX(
                $"Scene_{SceneName}_{sfx.name}",
                sfx
            );
        }
    }

    public bool IsActive => _isActive;
}