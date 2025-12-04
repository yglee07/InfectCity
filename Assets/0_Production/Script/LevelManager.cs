using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Prefabs (순서대로 Level1, Level2, Level3...)")]
    public GameObject[] levelPrefabs;

    private GameObject currentLevel;

    void Update()
    {
        // 키보드 숫자 1~9 입력 처리
        for (int i = 1; i <= levelPrefabs.Length; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                LoadLevel(i - 1); // 배열은 0부터니까 -1
            }
        }
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levelPrefabs.Length)
        {
            Debug.LogError($"레벨 인덱스 {index} 잘못됨!");
            return;
        }

        // 이전 레벨 제거
        if (currentLevel != null)
            Destroy(currentLevel);

        // 새 레벨 생성
        currentLevel = Instantiate(levelPrefabs[index]);
        currentLevel.name = $"Level_{index + 1}";

        Debug.Log($"Loaded Level {index + 1}");
    }
}
