using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple sound manager for loading and playing audio clips.
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private int poolSize = 10;

    [Header("Sound Library")]
    [SerializeField] private SoundEntry[] sounds;

    private Dictionary<string, AudioClip> soundLibrary = new Dictionary<string, AudioClip>();
    private List<AudioSource> audioPool = new List<AudioSource>();
    private int poolIndex = 0;

    [System.Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 2f)] public float pitchMin = 1f;
        [Range(0.5f, 2f)] public float pitchMax = 1f;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
        LoadSounds();
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            audioPool.Add(source);
        }
    }

    private void LoadSounds()
    {
        soundLibrary.Clear();
        if (sounds == null) return;

        foreach (var entry in sounds)
        {
            if (entry.clip != null && !string.IsNullOrEmpty(entry.id))
            {
                soundLibrary[entry.id] = entry.clip;
            }
        }
    }


    /// <summary>
    /// Play a sound by ID at a specific position.
    /// </summary>
    public void PlaySound(string soundId, Vector3 position, float volumeMultiplier = 1f)
    {
        SoundEntry entry = GetSoundEntry(soundId);
        if (entry == null || entry.clip == null) return;

        AudioSource source = GetPooledSource();
        source.transform.position = position;
        source.clip = entry.clip;
        source.volume = entry.volume * volumeMultiplier;
        source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        source.spatialBlend = 1f;
        source.Play();
    }

    /// <summary>
    /// Play a sound by ID (2D, no position).
    /// </summary>
    public void PlaySound(string soundId, float volumeMultiplier = 1f)
    {
        SoundEntry entry = GetSoundEntry(soundId);
        if (entry == null || entry.clip == null) return;

        AudioSource source = GetPooledSource();
        source.clip = entry.clip;
        source.volume = entry.volume * volumeMultiplier;
        source.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        source.spatialBlend = 0f;
        source.Play();
    }

    /// <summary>
    /// Play a clip directly at a position.
    /// </summary>
    public void PlayClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetPooledSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 1f;
        source.Play();
    }

    /// <summary>
    /// Play a random clip from an array at a position.
    /// </summary>
    public void PlayRandomClip(AudioClip[] clips, Vector3 position, float volume = 1f, float pitchVariation = 0.1f)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        PlayClip(clip, position, volume, pitch);
    }

    private SoundEntry GetSoundEntry(string soundId)
    {
        if (sounds == null) return null;
        foreach (var entry in sounds)
        {
            if (entry.id == soundId) return entry;
        }
        return null;
    }

    private AudioSource GetPooledSource()
    {
        AudioSource source = audioPool[poolIndex];
        poolIndex = (poolIndex + 1) % audioPool.Count;
        return source;
    }

    /// <summary>
    /// Register a sound at runtime.
    /// </summary>
    public void RegisterSound(string id, AudioClip clip, float volume = 1f)
    {
        if (clip == null || string.IsNullOrEmpty(id)) return;
        soundLibrary[id] = clip;
    }
}
