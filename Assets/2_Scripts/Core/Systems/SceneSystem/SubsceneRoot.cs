using System.Collections.Generic;
using UnityEngine;

public class SubsceneRoot : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string _sceneName;
    [SerializeField] private Sprite _background;
    [SerializeField] private bool _allowBackpack = true;

    [Header("Audio")]
    [SerializeField] private AudioClip _soundtrack;
    [SerializeField] private AudioClip[] _soundEffects;

    public Subscene GetSubscene()
    {
        var elements = new List<InteractiveElement>();
        foreach (Transform child in transform)
        {
            var element = child.GetComponent<InteractiveElement>();
            if (element) elements.Add(element);
        }

        return new Subscene
        {
            SceneName = _sceneName,
            RootObject = gameObject,
            Background = _background,
            InteractiveElements = elements,
            Soundtrack = _soundtrack,
            SoundEffects = new List<AudioClip>(_soundEffects),
            AllowBackpackAccess = _allowBackpack
        };
    }
}