using UnityEngine;

public class Lobby : MonoBehaviour
{
    public CountryDatabase countryDB;   // ← ScriptableObject 연결
    public Transform countryContainer;
    public Transform playerCube;
    public UILobby ui;

    private GameObject currentCountryInstance;

    public void RefreshLobby()
    {
        int stage = SaveSystem.Data.stage;
        CountryData info = countryDB.GetCountryByStage(stage);

        if (info == null)
        {
            Debug.LogError("해당 스테이지에 맞는 국가 없음: " + stage);
            return;
        }

        // 기존 국가 제거
        if (currentCountryInstance != null)
            Destroy(currentCountryInstance);

        // 새 국가 생성
        currentCountryInstance = Instantiate(info.prefab, countryContainer);

        // Center 찾기
        Transform center = currentCountryInstance.transform.Find("Center");
        if (center == null)
        {
            Debug.LogError($"{info.displayName} 프리팹에 Center 없음!");
            return;
        }

        // Cube 위치 이동
        playerCube.position = center.position + Vector3.up * 0.3f;

        // 진행률 계산
        int cleared = stage - info.startStage;  // 첫판 = 0%
        float progress = (float)cleared / info.TotalStages;

        // UI 갱신
        ui.UpdateCountryUI(info.displayName, progress,info.countryImage);

          UpdateStageDifficulty(stage);
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
