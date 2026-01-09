using System.Collections;
using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Instance;

    [Header("Managers")]
    public NPCManager npcManager;
    public DragInfectController dragInfector;
    public DragUnitController dragUnit;
    

  
    [SerializeField] private Level currentLevel;
    public Level CurrentLevel => currentLevel;
    public Transform CurrentLevelTransform => currentLevel != null ? currentLevel.transform : null;


    [Header("Level Root")]
    public Transform levelContainer;

    [Header("UI")]
    public UIGame uiGame;   

    public bool isPlaying = false;

 
   [SerializeField] float gameSpeed = 1f;
    public float GameSpeed => gameSpeed;

    const float BASE_FIXED_DT = 0.02f;
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

        //UpdateHUD();
        CheckStageClear();
        CheckStageFail();

    }
    public void SetGameSpeed(float speed)
    {
        gameSpeed = speed;

        Time.timeScale = gameSpeed;
        Time.fixedDeltaTime = BASE_FIXED_DT * gameSpeed;

        Debug.Log($"[Game] Speed = {gameSpeed}x");
    }

    public void ToggleSpeed()
    {
        SetGameSpeed(Mathf.Approximately(gameSpeed, 1f) ? 2f : 1f);
    }

    // =============================
    //        STAGE START
    // =============================
    public void StartStage()
    {
        NPCManager.Instance.combatMode = false;
        stageClearStarted = false;

        int stage = SaveSystem.Data.stage;


        LoadLevel(stage);

        //npcManager.SetupStage(stage);

        //dragInfector.ResetCharges();
        dragInfector.currentCharges = dragInfector.maxCharges; // ← 추가
        // =========================
        // 유닛 (언락 시에만)
        // =========================
        if (UnlockManager.IsUnlocked(UnlockType.DragUnit))
        {
            dragUnit.currentCharges = dragUnit.maxCharges;
        }
        else
        {
            dragUnit.currentCharges = 0;
        }

         uiGame.RefreshActionButtons(
        dragInfector.currentCharges,
        dragUnit.currentCharges
    );

        uiGame.SetStage(stage);
        uiGame.UpdateCharges(dragInfector.currentCharges, dragInfector.maxCharges);

        isPlaying = true;
    }

    // ========================================================
    //  LOAD LEVEL
    // ========================================================
    private void LoadLevel(int stage)
    {
        int index = (stage - 1) % GameManager.Instance.levelPrefabs.Length;

        if (currentLevel != null)
            Destroy(currentLevel.gameObject);

        Level inst = Instantiate(GameManager.Instance.levelPrefabs[index], levelContainer);
        inst.name = $"Level_{stage}";

        currentLevel = inst.GetComponent<Level>();

        CameraController cam = Camera.main.GetComponent<CameraController>();
        Debug.Log($"[LoadLevel] level={currentLevel.name}, startZoom={currentLevel.startZoom}, endZoom={currentLevel.endZoom}");
        cam.PlayIntro(
            currentLevel.startCameraPoint,
            currentLevel.endCameraPoint,
            currentLevel.startZoom,
            currentLevel.endZoom
        );
    }

    // =============================
    //        CLEAR CHECK
    // =============================
    private bool stageClearStarted = false;

    private void CheckStageClear()
    {
        //Debug.Log($"[CheckStageClear] Citizens: {npcManager.Citizens.Count}, Purple: {npcManager.PurpleZombies.Count}, CombatMode: {npcManager.combatMode}, stageClearStarted: {stageClearStarted}");

        // 시민 0 → Combat Mode ON
        if (npcManager.Citizens.Count == 0 && !npcManager.combatMode)
        {
           
            npcManager.combatMode = true;
        }

        // 승리 조건: 시민 0 + 보라 0 + 노랑 0 + 초록색이 1개 이상
        if (!stageClearStarted &&
            npcManager.Citizens.Count == 0 &&
            npcManager.PurpleZombies.Count == 0 &&
            npcManager.YellowZombies.Count == 0 &&
            npcManager.GreenZombies.Count > 0)
        {
        
            stageClearStarted = true;
            StartCoroutine(DelayedStageClear());
        }
    }

    private IEnumerator DelayedStageClear()
{
    // 전투 후 자연스러운 연출 시간
    yield return new WaitForSeconds(1f); // 0.5~1.0 추천

    StageClear();
}

    private void StageClear()
    {
        isPlaying = false;

        SoundManager.Instance?.PlaySFX("GameClear");

        Lobby lobby = GameManager.Instance.lobbyView.GetComponent<Lobby>();

        int clearedStage = SaveSystem.Data.stage;
        string countryId = lobby.GetCurrentCountryId(clearedStage);

        int beforeCleared = SaveSystem.Data.GetClearedStageCount(countryId);
        SaveSystem.Data.AddClearedStage(countryId);
        int afterCleared = SaveSystem.Data.GetClearedStageCount(countryId);

        // 🔥 실제 남은 그린 좀비 수
        int remainGreen =
            NPCManager.Instance.GreenZombies.Count;

        // 최소 1은 보장 (연출 안 비게)
        remainGreen = Mathf.Max(1, remainGreen);

        SaveSystem.Data.hasPendingConquerAnim = true;
        SaveSystem.Data.pendingCountryId = countryId;
        SaveSystem.Data.pendingBeforeCleared = beforeCleared;
        SaveSystem.Data.pendingAfterCleared = afterCleared;
        SaveSystem.Data.pendingGreenZombieCount = remainGreen;

        Debug.Log(
            $"[StageClear] {countryId} {beforeCleared}->{afterCleared} " +
            $"greenRemain={remainGreen}"
        );

        SaveSystem.Data.stage++;
        SaveSystem.Save();

        uiGame.ShowCompletePopup();
    }






    // =============================
    //        FAIL CHECK
    // =============================
    private void CheckStageFail()
    {
        bool allZero =
    npcManager.Citizens.Count == 0 &&
    npcManager.GreenZombies.Count == 0 &&
    npcManager.PurpleZombies.Count == 0 &&
    npcManager.YellowZombies.Count == 0;



        bool citizensRemain = npcManager.CurrentCitizenCount > 0;
        bool noCitizens = npcManager.CurrentCitizenCount == 0;

        bool noGreenZombies = npcManager.GreenZombies.Count == 0;
        bool hasPurpleZombies = npcManager.PurpleZombies.Count > 0;
        bool hasYellowZombies = npcManager.YellowZombies.Count > 0;

        bool noCharges = dragInfector.currentCharges <= 0 && dragUnit.currentCharges <= 0;

        bool case1_Blockaded =
            citizensRemain &&
            noGreenZombies &&
            noCharges;

        bool case2_NoFighterLeft =
            (hasPurpleZombies || hasYellowZombies) &&
            noCitizens &&
            noGreenZombies;

        if (case1_Blockaded || case2_NoFighterLeft || allZero)
        {
            StageFail();
        }
    }

    public void StageFail()
    {
        isPlaying = false;

        SoundManager.Instance?.PlaySFX("GameOver");

        uiGame.ShowFailedPopup();    // ← 실패 팝업
        //GameManager.Instance.ReturnToLobby();
           // 카메라 원위치 이동
      
    }
}
