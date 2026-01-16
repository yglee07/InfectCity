using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CountryNode;

public class Lobby : MonoBehaviour
{
 
    Dictionary<string, CountryNode> countryMap;
    List<string> countryOrder;
    public CountryNode currentCountry;

    public UILobby ui;


    [Header("Camera")]
    [SerializeField]
    Vector3 cameraOffset = new Vector3(0f, 5f, 0f);
 

    public NewsManager newsManager;
    bool newsPlayedThisEntry;

    [Header("Country Progress Order")]
    [SerializeField]
    List<CountryNode> countryProgressOrder;
    void Awake()
    {
        countryMap = new Dictionary<string, CountryNode>();
        countryOrder = new List<string>();

        foreach (var node in countryProgressOrder)
        {
            if (node == null) continue;

            countryMap[node.countryId] = node;
            countryOrder.Add(node.countryId);
        }

        Debug.Log($"[Lobby] Countries loaded: {countryOrder.Count}");
    }
    public void RefreshLobby()
    {
        newsPlayedThisEntry = false;
        Debug.Log("========== [Lobby] RefreshLobby START ==========");

        // 🔥 0️⃣ 로비 진입 즉시 스냅 (연출 없을 때만)
        if (!SaveSystem.Data.hasPendingConquerAnim)
        {
            // 아직 currentCountry 없으니, 임시로 계산 먼저
            CountryNode snapTarget = null;

            foreach (var id in countryOrder)
            {
                CountryNode node = countryMap[id];
                int cleared = GetClearedStageCount(node.countryId);

                if (cleared < node.stagesToConquer)
                {
                    snapTarget = node;
                    break;
                }
            }

            if (snapTarget == null)
                snapTarget = countryMap[countryOrder[countryOrder.Count - 1]];

            CameraController cam = Camera.main.GetComponent<CameraController>();
            float zoom = snapTarget.GetSuggestedZoom(1f);
            cam.SnapTo(snapTarget.center, cameraOffset, zoom);
        }

        // 1️⃣ 현재 국가 계산 (로직 그대로)
        CountryNode nextCountry = null;

        foreach (var id in countryOrder)
        {
            CountryNode node = countryMap[id];
            int cleared = GetClearedStageCount(node.countryId);

            if (cleared < node.stagesToConquer)
            {
                nextCountry = node;
                break;
            }
        }

        if (nextCountry == null)
            nextCountry = countryMap[countryOrder[countryOrder.Count - 1]];

        currentCountry = nextCountry;

        // ===============================
        // 2️⃣ 모든 국가 시각 상태 초기화
        // ===============================
        foreach (var kv in countryMap)
        {
            CountryNode node = kv.Value;
            int cleared = GetClearedStageCount(node.countryId);

            // 🔥 연출 대상 국가는 BEFORE 상태로 고정
            if (SaveSystem.Data.hasPendingConquerAnim &&
                node.countryId == SaveSystem.Data.pendingCountryId)
            {
                node.ApplyInstantProgress(
                    SaveSystem.Data.pendingBeforeCleared
                );
            }
            else
            {
                node.ApplyInstantProgress(cleared);
            }
        }
    
        // ===============================
        // 3️⃣ UI 초기값 세팅 (중요)
        // ===============================
        float uiProgress;

        if (SaveSystem.Data.hasPendingConquerAnim &&
            currentCountry.countryId == SaveSystem.Data.pendingCountryId)
        {
            // 🔥 연출 시작 전 값
            uiProgress =
                (float)SaveSystem.Data.pendingBeforeCleared
                / currentCountry.stagesToConquer;
        }
        else
        {
            int cleared = GetClearedStageCount(currentCountry.countryId);
            uiProgress =
                (float)cleared
                / currentCountry.stagesToConquer;
        }

        ui.UpdateCountryUI(
            currentCountry.countryId,
            uiProgress,
            currentCountry.countrySprite
        );

        UpdateStageDifficulty(SaveSystem.Data.stage);

        // 4️⃣ 연출은 맨 마지막
        TryPlayPendingConquerAnimation();


        TryShowNamePopupOnce();

        OnReturnedFromGame();

        Debug.Log("========== [Lobby] RefreshLobby END ==========");
    }
    void OnReturnedFromGame()
    {
        if (newsPlayedThisEntry) return;

        newsPlayedThisEntry = true;

        int cleared = SaveSystem.Data.GetClearedStageCount(currentCountry.countryId);
        int percent = Mathf.RoundToInt(
            (float)cleared / currentCountry.stagesToConquer * 100f
        );

        newsManager.PlayNews(
            SaveSystem.Data.stage,
            SaveSystem.Data.infectorName,
            currentCountry.countryId,
            percent
        );
    }
    void TryShowNamePopupOnce()
    {
        if (!string.IsNullOrEmpty(SaveSystem.Data.infectorName))
            return;

        ui.namePopup.SetActive(true);

        Debug.Log("[Lobby] First lobby entry → show name popup");
    }
    void TryPlayPendingConquerAnimation()
    {
        if (!SaveSystem.Data.hasPendingConquerAnim)
        {
            Debug.Log("[Lobby] No pending conquer animation.");
            return;
        }
    

        string countryId = SaveSystem.Data.pendingCountryId;

        if (!countryMap.TryGetValue(countryId, out CountryNode node))
        {
            Debug.Log("[Lobby] Pending country not found: " + countryId);
            return;
        }

        int steps = Mathf.Clamp(
     SaveSystem.Data.pendingGreenZombieCount,
     1,    // 최소 연출 보장
     60    // 최대 연출 제한
 );
        CameraController cam = Camera.main.GetComponent<CameraController>();

        // 🔥 1️⃣ 연출 시작 기준점으로 즉시 스냅
        float zoom = node.GetSuggestedZoom(1f);
        cam.SnapTo(node.center, cameraOffset, zoom);

        // 🔥 2️⃣ 연출 준비
        node.PrepareConquerStepAnimation(
            SaveSystem.Data.pendingBeforeCleared,
            SaveSystem.Data.pendingAfterCleared,
            steps
        );
        node.OnConquerProgressChanged = (fill) =>
        {
            ui.UpdateCountryProgress(fill);
        };
        // 🔥 3️⃣ 연출 종료 후 카메라 이동
        node.OnConquerAnimationFinished = () =>
        {
            node.OnConquerProgressChanged = null;
           
            StartCoroutine(MoveCameraToNextCountryDelayed(1f));
        };

        // 4️⃣ 좀비 생성
        SpawnConquerZombies(node, steps);

        SaveSystem.Data.hasPendingConquerAnim = false;
        SaveSystem.Save();
    }
    IEnumerator MoveCameraToNextCountryDelayed(float delay)
    {
        // ⏸ 여운 시간
        yield return new WaitForSeconds(delay);

        MoveCameraToNextCountry();
    }
    void MoveCameraToNextCountry()
    {
        // 다음 국가 계산
        CountryNode next = null;

        foreach (var id in countryOrder)
        {
            CountryNode node = countryMap[id];
            int cleared = GetClearedStageCount(node.countryId);

            if (cleared < node.stagesToConquer)
            {
                next = node;
                break;
            }
        }

        if (next == null)
            return;

        currentCountry = next;

        Debug.Log($"[Lobby] Camera moving to {currentCountry.countryId}");

        FocusCamera(currentCountry.center);

        // UI 갱신도 여기서
        float progress =
            (float)GetClearedStageCount(currentCountry.countryId)
            / currentCountry.stagesToConquer;

        ui.UpdateCountryUI(
            currentCountry.countryId,
            progress,
               currentCountry.countrySprite
        );
    }


    [SerializeField] GameObject greenZombiePrefab;
    void SpawnConquerZombies(CountryNode node, int count)
    {
        Debug.Log("Spawning " + count + " conquer zombies for " + node.countryId);

        const float WAIT_TIME = 1f;        // 거의 바로 시작
        const float EXPLODE_DURATION = 0.4f; // 전체 연출 1.2초 컷


        Transform center = node.center;
        float delayPerZombie = EXPLODE_DURATION / count;

        Bounds b = node.countryMesh.bounds;

        // 안쪽 여백
        float margin = 0.15f;

        float minX = b.min.x + b.size.x * margin;
        float maxX = b.max.x - b.size.x * margin;

        float minZ = b.min.z + b.size.z * margin;
        float maxZ = b.max.z - b.size.z * margin;

        // ⭐ 바닥 위로 살짝 띄우기
        float yOffset = 0.25f;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);

            Vector3 pos = new Vector3(
                x,
                b.max.y + yOffset,   // 또는 node.center.position.y + yOffset
                z
            );

            // 좀비는 바닥을 보게
            Quaternion rot = Quaternion.Euler(0f, 0f, 0f);

            GameObject zmb = Instantiate(
                greenZombiePrefab,
                pos,
                rot,
                node.center
            );

            float delay = WAIT_TIME + i * delayPerZombie;
            zmb.GetComponent<ConquerZombie>().Init(node, delay);
        }
    }








    public string GetCurrentCountryId(int stage)
    {
        int accumulated = 0;

        foreach (var id in countryOrder)
        {
            CountryNode node = countryMap[id];
            accumulated += node.stagesToConquer;

            if (stage <= accumulated)
                return id;
        }

        // fallback
        return countryOrder[countryOrder.Count - 1];
    }


    int GetClearedStageCount(string countryId)
    {
        return SaveSystem.Data.GetClearedStageCount(countryId);
    }

    void FocusCamera(Transform center)
    {
        CameraController cam = Camera.main.GetComponent<CameraController>();
        float zoom = currentCountry.GetSuggestedZoom(1f);
        cam.FocusOn(center, cameraOffset, zoom);
    }

    void UpdateStageDifficulty(int stage)
{
    Debug.Log($"[Diff] UpdateStageDifficulty START | stage = {stage}");



    // 2️⃣ levelPrefabs 체크
    if (GameManager.Instance.levelPrefabs == null)
    {
        Debug.LogError("[Diff] game.levelPrefabs is NULL");
        return;
    }

    if (GameManager.Instance.levelPrefabs.Length == 0)
    {
        Debug.LogError("[Diff] game.levelPrefabs.Length == 0");
        return;
    }

    Debug.Log($"[Diff] levelPrefabs.Length = {GameManager.Instance.levelPrefabs.Length}");

    // 3️⃣ index 계산
    int index = (stage - 1) % GameManager.Instance.levelPrefabs.Length;
    Debug.Log($"[Diff] calculated index = {index}");

    if (index < 0 || index >= GameManager.Instance.levelPrefabs.Length)
    {
        Debug.LogError($"[Diff] index OUT OF RANGE: {index}");
        return;
    }

    // 4️⃣ levelPrefab 체크
    Level levelPrefab = GameManager.Instance.levelPrefabs[index];
    if (levelPrefab == null)
    {
        Debug.LogError($"[Diff] levelPrefab is NULL at index {index}");
        return;
    }

    Debug.Log($"[Diff] levelPrefab name = {levelPrefab.name}");

    // 5️⃣ Level 컴포넌트 체크
    Level level = levelPrefab.GetComponent<Level>();
    if (level == null)
    {
        Debug.LogError($"[Diff] Level component MISSING on prefab: {levelPrefab.name}");
        return;
    }

    Debug.Log($"[Diff] Level.difficulty = {level.difficulty}");

    // 6️⃣ UI 체크
    if (ui == null)
    {
        Debug.LogError("[Diff] UILobby ui reference is NULL");
        return;
    }

    if (ui.difficultyText == null)
    {
        Debug.LogError("[Diff] ui.difficultyText is NULL");
        return;
    }

    // 7️⃣ 최종 적용
    ui.UpdateDifficulty(level.difficulty);
    Debug.Log("[Diff] UpdateDifficulty CALLED SUCCESSFULLY");
}

    
    public void SkipConquerAnimation()
    {
        Debug.Log("🔥 SkipConquerAnimation FORCE CLEANUP");

        // 🔥 연출용 ConquerZombie 무조건 제거
        ConquerZombie[] zombies =
            GetComponentsInChildren<ConquerZombie>(true);

        Debug.Log($"[Skip] Found {zombies.Length} ConquerZombies");

        foreach (var z in zombies)
        {
            Debug.Log($"[Skip] Destroy {z.name}");
            Destroy(z.gameObject);
        }

        // 🔥 색상은 결과 기준으로 확정
        if (!string.IsNullOrEmpty(SaveSystem.Data.pendingCountryId) &&
            countryMap.TryGetValue(
                SaveSystem.Data.pendingCountryId,
                out CountryNode node
            ))
        {
            node.ApplyInstantProgress(
                SaveSystem.Data.pendingAfterCleared
            );
        }

        // 🔥 flag는 그냥 정리 차원에서 false
        SaveSystem.Data.hasPendingConquerAnim = false;
        SaveSystem.Save();
    }


}
