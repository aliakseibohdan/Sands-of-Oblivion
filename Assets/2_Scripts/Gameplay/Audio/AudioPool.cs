using UnityEngine;
using System.Collections.Generic;

public class AudioPool
{
    private readonly Queue<AudioSource> _pool = new();
    private readonly Transform _parent;
    private readonly SoundConfig _config;

    public AudioPool(SoundConfig config, Transform parent)
    {
        _config = config;
        _parent = parent;

        // Prewarm pool
        for (int i = 0; i < _config.InitialPoolSize; i++)
        {
            _pool.Enqueue(CreateNewSource());
        }
    }

    public AudioSource GetSource()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }

        if (_pool.Count < _config.MaxPoolSize)
        {
            return CreateNewSource();
        }

        Debug.LogWarning("Audio pool exhausted!");
        return null;
    }

    public void ReturnSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        _pool.Enqueue(source);
    }

    private AudioSource CreateNewSource()
    {
        GameObject go = new GameObject("AudioSource_Pooled");
        go.transform.SetParent(_parent);
        go.SetActive(false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = _config.MainMixer.FindMatchingGroups("SFX")[0];

        return source;
    }
}