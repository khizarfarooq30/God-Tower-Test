using System;
using System.Collections.Generic;
using UnityEngine;

public enum SoundId { Climb, Grab, Hit, Fall, Recover, PopOutWarning, PopOut, Victory, FinalVictory, ButtonTap }

public class GameAudio : MonoBehaviour
{
    [Serializable]
    private class SoundEntry
    {
        public SoundId Id;
        public AudioClip[] Clips;
        [Range(0f, 1f)] public float Volume = 1f;
        public Vector2 PitchRange = Vector2.one;
    }

    private static GameAudio instance;

    [SerializeField] private SoundEntry[] sounds;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private Transform proximityTarget;
    [SerializeField, Min(1)] private int voices = 6;

    private readonly Dictionary<SoundId, SoundEntry> lookup = new Dictionary<SoundId, SoundEntry>();
    private AudioSource[] sources;
    private int nextSource;

    public float SfxVolume
    {
        get => sfxVolume;
        set => sfxVolume = Mathf.Clamp01(value);
    }

    public static void Play(SoundId id)
    {
        if (instance != null)
            instance.PlayInternal(id);
    }

    public static void PlayNear(SoundId id, Vector3 position, float verticalRange)
    {
        if (instance == null)
            return;

        Transform target = instance.proximityTarget;
        if (target != null && Mathf.Abs(position.y - target.position.y) > verticalRange)
            return;

        instance.PlayInternal(id);
    }

    private void Reset()
    {
        var ids = (SoundId[])Enum.GetValues(typeof(SoundId));
        sounds = new SoundEntry[ids.Length];
        for (int i = 0; i < ids.Length; i++)
            sounds[i] = new SoundEntry { Id = ids[i], Volume = DefaultVolume(ids[i]), PitchRange = DefaultPitch(ids[i]) };
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(GameAudio)} on '{name}' disabled.", this);
            enabled = false;
            return;
        }

        instance = this;

        if (sounds != null)
        {
            foreach (SoundEntry entry in sounds)
                lookup[entry.Id] = entry;
        }

        sources = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sources[i] = source;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void PlayInternal(SoundId id)
    {
        if (!enabled || !lookup.TryGetValue(id, out SoundEntry entry) || entry.Clips == null || entry.Clips.Length == 0)
            return;

        AudioClip clip = entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
        if (clip == null)
            return;

        AudioSource source = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;

        float minPitch = entry.PitchRange.x > 0f ? entry.PitchRange.x : 1f;
        float maxPitch = entry.PitchRange.y > 0f ? entry.PitchRange.y : minPitch;
        source.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip, entry.Volume * sfxVolume);
    }

    private static float DefaultVolume(SoundId id)
    {
        switch (id)
        {
            case SoundId.Climb: return 0.25f;
            case SoundId.Grab: return 0.35f;
            case SoundId.Fall: return 0.5f;
            case SoundId.Recover: return 0.5f;
            case SoundId.PopOutWarning: return 0.4f;
            case SoundId.PopOut: return 0.6f;
            case SoundId.ButtonTap: return 0.6f;
            default: return 0.9f;
        }
    }

    private static Vector2 DefaultPitch(SoundId id)
    {
        switch (id)
        {
            case SoundId.Climb:
            case SoundId.Grab:
                return new Vector2(0.9f, 1.15f);
            case SoundId.Hit:
            case SoundId.PopOut:
                return new Vector2(0.95f, 1.05f);
            default:
                return Vector2.one;
        }
    }
}
