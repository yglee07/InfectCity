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
        int total = levelPrefabs.Length;

        // stage는 그대로 사용하되,
        // 프리팹은 modulo로 반복
        int index = (stage - 1) % total;

        // 기존 레벨 삭제
        if (currentLevel != null)
            Destroy(currentLevel);

        // 새 레벨 생성
        currentLevel = Instantiate(
    levelPrefabs[index],
    Game.Instance.levelContainer 
);
        currentLevel.name = $"Level_{stage}";
    }
}
