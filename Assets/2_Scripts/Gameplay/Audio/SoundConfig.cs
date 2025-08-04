using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SoundConfig", menuName = "Audio/Sound Config")]
public class SoundConfig : ScriptableObject
{
    [Header("Mixer References")]
    public AudioMixer MainMixer;

    [Header("Volume Parameters")]
    public string MasterVolumeParam = "MasterVolume";
    public string MusicVolumeParam = "MusicVolume";
    public string SFXVolumeParam = "SFXVolume";

    [Header("Default Volumes")]
    [Range(0f, 1f)] public float DefaultMasterVolume = 0.8f;
    [Range(0f, 1f)] public float DefaultMusicVolume = 0.7f;
    [Range(0f, 1f)] public float DefaultSFXVolume = 0.9f;

    [Header("Audio Pooling")]
    public int InitialPoolSize = 10;
    public int MaxPoolSize = 20;

    [Header("Fade Settings")]
    public float MusicFadeDuration = 2f;
    public float SFXFadeDuration = 0.5f;
}