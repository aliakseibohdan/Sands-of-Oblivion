using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private SoundConfig _config;

    private AudioSource _musicSource;
    private AudioSource _ambienceSource;
    private AudioPool _sfxPool;
    private Dictionary<string, AudioSource> _loopingSfx = new();

    private Coroutine _musicFadeCoroutine;
    private float _currentMusicVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        // Create dedicated music source
        GameObject musicGO = new("MusicSource");
        musicGO.transform.SetParent(transform);
        _musicSource = musicGO.AddComponent<AudioSource>();
        _musicSource.outputAudioMixerGroup = _config.MainMixer.FindMatchingGroups("Music")[0];
        _musicSource.loop = true;

        // Create ambience source
        GameObject ambienceGO = new("AmbienceSource");
        ambienceGO.transform.SetParent(transform);
        _ambienceSource = ambienceGO.AddComponent<AudioSource>();
        _ambienceSource.outputAudioMixerGroup = _config.MainMixer.FindMatchingGroups("Ambience")[0];
        _ambienceSource.loop = true;

        // Initialize SFX pool
        GameObject poolParent = new("SFXPool");
        poolParent.transform.SetParent(transform);
        _sfxPool = new AudioPool(_config, poolParent.transform);

        // Set initial volumes
        SetMixerVolume(_config.MasterVolumeParam, _config.DefaultMasterVolume);
        SetMixerVolume(_config.MusicVolumeParam, _config.DefaultMusicVolume);
        SetMixerVolume(_config.SFXVolumeParam, _config.DefaultSFXVolume);
    }

    // ======== PUBLIC INTERFACE ========
    public void PlayMusic(AudioClip clip, float volume = 1f, bool fade = true)
    {
        if (clip == null) return;

        // Skip if same clip is playing
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _currentMusicVolume = volume;

        if (_musicFadeCoroutine != null)
            StopCoroutine(_musicFadeCoroutine);

        _musicFadeCoroutine = StartCoroutine(
            fade ?
            CrossfadeMusic(clip, volume) :
            SwitchMusicImmediate(clip, volume)
        );
    }

    public void PlayAmbience(AudioClip clip, float volume = 0.8f)
    {
        if (clip == null) return;

        _ambienceSource.clip = clip;
        _ambienceSource.volume = volume;
        _ambienceSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        var source = _sfxPool.GetSource();
        if (source == null) return;

        source.gameObject.SetActive(true);
        source.clip = clip;
        source.volume = volume;
        source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        source.Play();

        StartCoroutine(ReturnToPoolAfterPlay(source));
    }

    public void PlayLoopingSFX(string id, AudioClip clip, float volume = 1f)
    {
        if (_loopingSfx.TryGetValue(id, out var existingSource))
        {
            // Adjust existing source
            existingSource.volume = volume;
            return;
        }

        var source = _sfxPool.GetSource();
        if (source == null) return;

        source.gameObject.SetActive(true);
        source.name = $"LoopingSFX_{id}";
        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.Play();

        _loopingSfx[id] = source;
    }

    public void StopLoopingSFX(string id)
    {
        if (_loopingSfx.TryGetValue(id, out var source))
        {
            source.Stop();
            _sfxPool.ReturnSource(source);
            _loopingSfx.Remove(id);
        }
    }

    public void SetMixerVolume(string parameter, float normalizedVolume)
    {
        if (_config.MainMixer == null) return;

        // Convert 0-1 range to -80dB to 0dB
        float dB = normalizedVolume > 0.01f ?
            20f * Mathf.Log10(normalizedVolume) :
            -80f;

        _config.MainMixer.SetFloat(parameter, dB);
    }

    // ======== VOLUME CONTROL ========
    public void SetMasterVolume(float normalizedVolume) =>
        SetMixerVolume(_config.MasterVolumeParam, normalizedVolume);

    public void SetMusicVolume(float normalizedVolume) =>
        SetMixerVolume(_config.MusicVolumeParam, normalizedVolume);

    public void SetSFXVolume(float normalizedVolume) =>
        SetMixerVolume(_config.SFXVolumeParam, normalizedVolume);

    // ======== PRIVATE UTILITIES ========
    private IEnumerator CrossfadeMusic(AudioClip newClip, float targetVolume)
    {
        // Fade out current music
        float elapsed = 0;
        float startVolume = _musicSource.volume;

        while (elapsed < _config.MusicFadeDuration)
        {
            _musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / _config.MusicFadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Switch clip
        _musicSource.clip = newClip;
        _musicSource.Play();

        // Fade in new music
        elapsed = 0;
        while (elapsed < _config.MusicFadeDuration)
        {
            _musicSource.volume = Mathf.Lerp(0, targetVolume, elapsed / _config.MusicFadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _musicSource.volume = targetVolume;
    }

    private IEnumerator SwitchMusicImmediate(AudioClip newClip, float volume)
    {
        _musicSource.Stop();
        _musicSource.clip = newClip;
        _musicSource.volume = volume;
        _musicSource.Play();
        yield break;
    }

    private IEnumerator ReturnToPoolAfterPlay(AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        _sfxPool.ReturnSource(source);
    }

    // ======== EDITOR UTILITIES ========
#if UNITY_EDITOR
    [ContextMenu("Print Audio Status")]
    private void PrintAudioStatus()
    {
        Debug.Log($"Music: {_musicSource.clip?.name ?? "None"} " +
                  $"{(_musicSource.isPlaying ? "PLAYING" : "STOPPED")}");
        Debug.Log($"Ambience: {_ambienceSource.clip?.name ?? "None"} " +
                  $"{(_ambienceSource.isPlaying ? "PLAYING" : "STOPPED")}");
        Debug.Log($"Looping SFX: {_loopingSfx.Count}");
    }
#endif
}