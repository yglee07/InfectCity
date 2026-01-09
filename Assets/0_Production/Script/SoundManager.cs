using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Scream Clips")]
    public AudioClip femaleScream;
    public AudioClip maleScream;

    [Header("Scream Settings")]
    [Range(0f, 1f)] public float screamVolume = 0.15f;
    public int maxSimultaneousScreams = 5;

    [Header("3D Sound Tuning")]
    public float minDistance = 40f;   // 이 거리까지 볼륨 100%
    public float maxDistance = 300f;  // 사실상 맵 전체 커버

    AudioSource[] screamSources;
    int screamIndex = 0;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        screamSources = new AudioSource[maxSimultaneousScreams];

        for (int i = 0; i < screamSources.Length; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;

            // 🔥 실전 핵심 세팅
            src.spatialBlend = 1f; // 3D
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.volume = screamVolume;

            screamSources[i] = src;
        }
    }

    // ============================
    // 시민 비명 재생
    // ============================
    public void PlayCitizenScream(Vector3 worldPos)
    {
        AudioClip clip =
            Random.value < 0.5f
            ? femaleScream
            : maleScream;

        AudioSource src = screamSources[screamIndex];

        // 이미 재생 중이면 다음 슬롯으로
        if (src.isPlaying)
        {
            screamIndex = (screamIndex + 1) % screamSources.Length;
            src = screamSources[screamIndex];
        }

        src.transform.position = worldPos;
        src.clip = clip;
        src.Play();

        screamIndex = (screamIndex + 1) % screamSources.Length;
    }
}
