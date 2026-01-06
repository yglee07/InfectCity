using System.Collections.Generic;
using UnityEngine;
using static CountryNode;

public class Lobby : MonoBehaviour
{
    //public CountryDatabase countryDB;   // ← ScriptableObject 연결
    public Transform countryContainer;
    public Material normalMat;
    public Material selectedMat;
    public Material conqueredMat;
    Dictionary<string, CountryNode> countryMap;
    List<string> countryOrder;
    CountryNode currentCountry;

    public UILobby ui;

    private GameObject currentCountryInstance;
    [Header("Camera")]
    [SerializeField]
    Vector3 cameraOffset = new Vector3(0f, 0f, -10f);
    void Awake()
    {
        countryMap = new Dictionary<string, CountryNode>();
        countryOrder = new List<string>();

        foreach (Transform child in countryContainer)
        {
            CountryNode node = child.GetComponent<CountryNode>();
            if (node == null) continue;

            countryMap[node.countryId] = node;
            countryOrder.Add(node.countryId);
        }

        Debug.Log($"[Lobby] Countries loaded: {countryOrder.Count}");
    }
    public void RefreshLobby()
    {
        int stage = SaveSystem.Data.stage;
        Debug.Log("========== [Lobby] RefreshLobby START ==========");
        Debug.Log($"[Lobby] current stage = {stage}");
        // 1️⃣ 현재 국가 계산
        int countryIndex = stage / 2;
        countryIndex = Mathf.Clamp(countryIndex, 0, countryOrder.Count - 1);
        string countryId = countryOrder[countryIndex];
        currentCountry = countryMap[countryId];
        Debug.Log($"[Lobby] currentCountry = {countryId}");
        // 2️⃣ 모든 국가 상태 초기화
        foreach (var kv in countryMap)
        {
            CountryNode node = kv.Value;
            int cleared = GetClearedStageCount(node.countryId);
            Debug.Log($"[Lobby] countryId={node.countryId}, cleared={cleared}");
            if (cleared >= 2)
            {
                Debug.Log($"[Lobby] {node.countryId} -> CONQUERED");
                node.SetState(
                    CountryState.Conquered,
                    normalMat,
                    selectedMat,
                    conqueredMat
                );
            }
            else
            {
                node.SetState(
                    CountryState.Normal,
                    normalMat,
                    selectedMat,
                    conqueredMat
                );
            }
        }

        // 3️⃣ 현재 국가 → Selected (단, 이미 정복이면 제외)
        int currentCleared = GetClearedStageCount(currentCountry.countryId);
        Debug.Log($"[Lobby] currentCountry cleared = {currentCleared}");
        if (currentCleared < 2)
        {
            Debug.Log($"[Lobby] {currentCountry.countryId} -> SELECTED");
            currentCountry.SetState(
                CountryState.Selected,
                normalMat,
                selectedMat,
                conqueredMat
            );
        }

        // 4️⃣ 카메라
        FocusCamera(currentCountry.center);

        // 5️⃣ UI
        float progress = currentCleared / 2f;
        ui.UpdateCountryUI(
            currentCountry.countryId,
            progress,
            null
        );

        UpdateStageDifficulty(stage);

        Debug.Log("========== [Lobby] RefreshLobby END ==========");
    }

    public string GetCurrentCountryId(int stage)
    {
        int countryIndex = stage / 2;
        countryIndex = Mathf.Clamp(countryIndex, 0, countryOrder.Count - 1);
        return countryOrder[countryIndex];
    }
    void ApplyCountryColor(CountryNode node)
    {
        int cleared = GetClearedStageCount(node.countryId);

        node.countryMesh.material =
            (cleared >= 2) ? conqueredMat : normalMat;
    }

    int GetClearedStageCount(string countryId)
    {
        if (SaveSystem.Data.countryStageCount.TryGetValue(countryId, out int v))
            return v;
        return 0;
    }

    void FocusCamera(Transform center)
    {
        CameraController cam = Camera.main.GetComponent<CameraController>();
        float zoom = currentCountry.GetSuggestedZoom(1.15f);
        // 또는 기본값이 보통 1.2f
        cam.FocusOn(currentCountry.center, cameraOffset, zoom); 
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

  
}
