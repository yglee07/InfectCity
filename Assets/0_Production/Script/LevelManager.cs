using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    void Awake()
    {
        Instance = this;
    }

    [Header("Level Prefabs")]
    public GameObject[] levelPrefabs;

    private GameObject currentLevel;

    public void LoadLevelByStage(int stage)
    {
        // stage 1 → index 0 로 변환
        int index = stage - 1;

        if (index < 0 || index >= levelPrefabs.Length)
        {
            Debug.LogError($"LoadLevelByStage 실패! stage={stage} index={index} 범위 밖");
            return;
        }

        // 이전 레벨 제거
        if (currentLevel != null)
            Destroy(currentLevel);

        // 새 레벨 생성
        currentLevel = Instantiate(levelPrefabs[index]);
        currentLevel.name = $"Level_{stage}";
    }
}
