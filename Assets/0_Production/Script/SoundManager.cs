using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    // ============================
    // 데이터 구조
    // ============================
    [System.Serializable]
    public class ScreamClip
    {
        public AudioClip clip;

        [Range(0f, 1.5f)]
        public float volumeMultiplier = 1f;
    }

    // ============================
    // Scream Clips
    // ============================
    [Header("Female Screams")]
    public ScreamClip[] femaleScreams;

    [Header("Male Screams")]
    public ScreamClip[] maleScreams;

    // ============================
    // Global Settings
    // ============================
    [Header("Global Volume")]
    [Range(0f, 1f)]
    public float screamVolume = 0.15f;

    [Header("Pooling")]
    public int maxSimultaneousScreams = 5;

    [Header("Cooldown")]
    public float screamCooldown = 0.15f;

    [Header("Pitch Random")]
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    [Header("3D Sound")]
    public float minDistance = 40f;
    public float maxDistance = 300f;

    // ============================
    // Internal
    // ============================
    AudioSource[] screamSources;
    int screamIndex = 0;
    float lastScreamTime = -999f;
    int lastPlayedFrame = -1;

    // ============================
    // Init
    // ============================
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

            // 🔥 실전 3D 세팅
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;

            screamSources[i] = src;
        }
    }

    // ============================
    // Public API
    // ============================
    public void PlayCitizenScream(Vector3 worldPos)
    {
        // 1️⃣ 프레임 중복 방지
        if (Time.frameCount == lastPlayedFrame)
            return;

        // 2️⃣ 쿨타임
        if (Time.time - lastScreamTime < screamCooldown)
            return;

        lastPlayedFrame = Time.frameCount;
        lastScreamTime = Time.time;

        // 3️⃣ 클립 선택
        ScreamClip data = PickRandomScream();
        if (data == null || data.clip == null)
            return;

        // 4️⃣ 오디오 소스 선택
        AudioSource src = screamSources[screamIndex];

        if (src.isPlaying)
        {
            screamIndex = (screamIndex + 1) % screamSources.Length;
            src = screamSources[screamIndex];
        }

        screamIndex = (screamIndex + 1) % screamSources.Length;

        // 5️⃣ 거리 기반 볼륨 (선택적이지만 안정감 ↑)
        float dist = Vector3.Distance(
            Camera.main.transform.position,
            worldPos
        );

        float distanceFactor = Mathf.InverseLerp(
            maxDistance,
            minDistance,
            dist
        );

        // 6️⃣ 세팅
        src.transform.position = worldPos;
        src.clip = data.clip;
        src.pitch = Random.Range(pitchRange.x, pitchRange.y);
        src.volume = screamVolume * data.volumeMultiplier * distanceFactor;

        // 7️⃣ 재생
        src.Play();
    }

    // ============================
    // Helpers
    // ============================
    ScreamClip PickRandomScream()
    {
        bool female = Random.value < 0.5f;

        ScreamClip[] pool = female ? femaleScreams : maleScreams;
        if (pool == null || pool.Length == 0)
            return null;

        return pool[Random.Range(0, pool.Length)];
    }
}
