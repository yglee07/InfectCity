using System.Collections;
using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Instance;

    [Header("Managers")]
    public NPCManager npcManager;
    public DragInfectController dragInfector;

    [Header("Level Prefabs")]
    public GameObject[] levelPrefabs;

  
    [SerializeField] private Level currentLevel;
    public Level CurrentLevel => currentLevel;
    public Transform CurrentLevelTransform => currentLevel != null ? currentLevel.transform : null;


    [Header("Level Root")]
    public Transform levelContainer;

    [Header("UI")]
    public UIGame uiGame;   

    public bool isPlaying = false;

 

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

        uiGame.SetStage(stage);
        uiGame.UpdateCharges(dragInfector.currentCharges, dragInfector.maxCharges);

        isPlaying = true;
    }

    // ========================================================
    //  LOAD LEVEL
    // ========================================================
    private void LoadLevel(int stage)
    {
        int index = (stage - 1) % levelPrefabs.Length;

        if (currentLevel != null)
            Destroy(currentLevel.gameObject);

        GameObject inst = Instantiate(levelPrefabs[index], levelContainer);
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

        // 승리 조건: 시민 0 + 보라 0
        if (!stageClearStarted &&
            npcManager.Citizens.Count == 0 &&
            npcManager.PurpleZombies.Count == 0)
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
    uiGame.ShowCompletePopup();
}


    // =============================
    //        FAIL CHECK
    // =============================
    private void CheckStageFail()
    {
        bool citizensRemain = npcManager.CurrentCitizenCount > 0;
        bool noCitizens = npcManager.CurrentCitizenCount == 0;

        bool noGreenZombies = npcManager.GreenZombies.Count == 0;
        bool hasPurpleZombies = npcManager.PurpleZombies.Count > 0;

        bool noCharges = dragInfector.currentCharges <= 0;

        bool case1_Blockaded =
            citizensRemain &&
            noGreenZombies &&
            noCharges;

        bool case2_NoFighterLeft =
            hasPurpleZombies &&
            noCitizens &&
            noGreenZombies;

        if (case1_Blockaded || case2_NoFighterLeft)
        {
            StageFail();
        }
    }

    public void StageFail()
    {
        isPlaying = false;
        uiGame.ShowFailedPopup();    // ← 실패 팝업
        //GameManager.Instance.ReturnToLobby();
           // 카메라 원위치 이동
      
    }
}
