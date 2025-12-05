using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Instance;

    [Header("Managers")]
    public NPCManager npcManager;
    public DragInfectController dragInfector;

    [Header("Level Prefabs")]
    public GameObject[] levelPrefabs;

    private GameObject currentLevel;

    [Header("Level Root")]
    public Transform levelContainer;

    [Header("UI")]
    public UIGame uiGame;   

    public bool isPlaying = false;

    public Transform CurrentLevelTransform
    {
        get
        {
            return currentLevel != null ? currentLevel.transform : null;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        StartStage();
    }

    void Update()
    {
        if (!isPlaying) return;

        UpdateHUD();
        CheckStageClear();
    }


    // =============================
    //        STAGE START
    // =============================
    public void StartStage()
    {
        int stage = SaveSystem.Data.stage;


        LoadLevel(stage);
        //npcManager.SetupStage(stage);

        //dragInfector.ResetCharges();
        dragInfector.currentCharges = dragInfector.maxCharges; // ← 추가

        uiGame.SetStage(stage);
        uiGame.UpdateCharges(dragInfector.currentCharges, dragInfector.maxCharges);

        isPlaying = true;
    }

    // ========================================================
    //  LOAD LEVEL
    // ========================================================
    private void LoadLevel(int stage)
    {
        int total = levelPrefabs.Length;

        // Prefab index는 반복
        int index = (stage - 1) % total;

        // 기존 레벨 삭제
        if (currentLevel != null)
            Destroy(currentLevel);

        // 새 레벨 생성 → LevelContainer의 child
        currentLevel = Instantiate(levelPrefabs[index], levelContainer);
        currentLevel.name = $"Level_{stage}";
    }


    // =============================
    //         UPDATE HUD
    // =============================
    private void UpdateHUD()
    {
        uiGame.UpdateInfectionProgress(npcManager.InfectionProgress);
    }


    // =============================
    //        CLEAR CHECK
    // =============================
    private void CheckStageClear()
    {
        if (npcManager.IsStageClear())
        {
            StageClear();
        }
    }

    private void StageClear()
    {
        isPlaying = false;
        uiGame.ShowCompletePopup();
        //GameManager.Instance.OnGameClear();
    }


    // =============================
    //        FAIL CHECK
    // =============================
    public void StageFail()
    {
        isPlaying = false;
        uiGame.ShowFailedPopup();    // ← 실패 팝업
        //GameManager.Instance.ReturnToLobby();
    }
}
