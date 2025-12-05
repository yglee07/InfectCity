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
        ui.UpdateCountryUI(info.displayName, progress);
    }

    public void OnClickPlay()
    {
        GameManager.Instance.StartGame();
    }
}
