using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Global Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Header("SFX Entries")]
    public SFXEntry[] sfxEntries;

    Dictionary<string, SFXEntry> entryMap;
    Dictionary<string, SFXRuntime> runtimeMap;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        entryMap = new Dictionary<string, SFXEntry>();
        runtimeMap = new Dictionary<string, SFXRuntime>();

        foreach (var entry in sfxEntries)
        {
            if (string.IsNullOrEmpty(entry.key))
                continue;

            int poolSize = Mathf.Max(1, entry.maxSimultaneous);
            entryMap[entry.key] = entry;

            SFXRuntime rt = new SFXRuntime
            {
                sources = new AudioSource[poolSize],
                index = 0,
                lastPlayTime = -999f,
                lastPlayedFrame = -1
            };

            for (int i = 0; i < rt.sources.Length; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;

                if (entry.is3D)
                {
                    src.spatialBlend = 1f;
                    src.rolloffMode = AudioRolloffMode.Linear;
                    src.minDistance = entry.minDistance;
                    src.maxDistance = entry.maxDistance;
                    src.dopplerLevel = 0f;
                }
                else
                {
                    src.spatialBlend = 0f;
                }

                rt.sources[i] = src;
            }

            runtimeMap[entry.key] = rt;
        }
    }

    // ============================
    // Public API
    // ============================
    public void PlaySFX(string key, Vector3? worldPos = null)
    {
        if (!entryMap.TryGetValue(key, out var entry))
            return;

        SFXRuntime rt = runtimeMap[key];

        // 프레임 중복 방지
        if (Time.frameCount == rt.lastPlayedFrame)
            return;

        // 쿨타임
        if (Time.time - rt.lastPlayTime < entry.cooldown)
            return;

        rt.lastPlayedFrame = Time.frameCount;
        rt.lastPlayTime = Time.time;

        // 클립 선택
        if (entry.clips == null || entry.clips.Length == 0)
            return;

        SFXClip clipData =
            entry.clips[Random.Range(0, entry.clips.Length)];

        if (clipData.clip == null)
            return;

        // AudioSource 선택
        AudioSource src = rt.sources[rt.index];

        if (src.isPlaying)
        {
            rt.index = (rt.index + 1) % rt.sources.Length;
            src = rt.sources[rt.index];
        }

        rt.index = (rt.index + 1) % rt.sources.Length;

        // 세팅
        if (entry.is3D && worldPos.HasValue)
        {
            src.transform.position = worldPos.Value;

        }
        src.clip = clipData.clip;
        src.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y);
        src.volume = masterVolume * clipData.volumeMultiplier;

        src.Play();
    }
}
class SFXRuntime
{
    public AudioSource[] sources;
    public int index;
    public float lastPlayTime;
    public int lastPlayedFrame;
}

[System.Serializable]
public class SFXClip
{
    public AudioClip clip;

    [Range(0f, 2f)]
    public float volumeMultiplier = 1f;
}

[System.Serializable]
public class SFXEntry
{
    public string key;

    [Header("Clips")]
    public SFXClip[] clips;

    [Header("Pooling")]
    public int maxSimultaneous = 3;

    [Header("Cooldown")]
    public float cooldown = 0f;

    [Header("Pitch Random")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("3D Sound")]
    public bool is3D = false;
    public float minDistance = 40f;
    public float maxDistance = 300f;
}
