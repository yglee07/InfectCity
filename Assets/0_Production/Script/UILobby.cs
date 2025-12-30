using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILobby : MonoBehaviour
    {[Header("Difficulty")]
    public GameObject difficultyBar;   // 전체 바 오브젝트
    public TMP_Text difficultyText;

    [Header("Texts")]
    public TMP_Text countryNameText;   // 국가명
    public Image countryImage;
    public TMP_Text percentText;       // 퍼센트
    public TMP_Text levelText;         // "Level 999"
    public TMP_Text coinText;     // 🔥 코인 UI 추가

    [Header("Gauge")]
    public Slider gaugeSlider;

    [Header("Buttons")]
    public Button startButton;         // ← Start 버튼

    [Header("Upgrade Buttons")]
    public Button moveSpeedButton;
    public Button radiusButton;
    public Button mutateChanceButton;

    [Header("Upgrade Texts")]
    public TMP_Text moveSpeedText;
    public TMP_Text radiusText;
    public TMP_Text mutateChanceText;



    void Start()
    {
        // 버튼 클릭 시 StartGame 호출
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnClickStart);
        }

        if(moveSpeedButton !=null)
        {
            moveSpeedButton.onClick.AddListener(OnUpgradeMoveSpeed);
        }
        if(radiusButton !=null)
        {
            radiusButton.onClick.AddListener(OnUpgradeRadius);
        }
        //if(mutateChanceButton !=null)
        //{
        //    mutateChanceButton.onClick.AddListener(OnUpgradeMutate);
        //}

      
    }

    public void UpdateCountryUI(string countryName, float progress, Sprite countrySprite)
    {

        // 국가명
        countryNameText.text = countryName;

        // 퍼센트
        int pct = Mathf.RoundToInt(progress * 100f);
        percentText.text = pct + "%";

        // 게이지
        if (gaugeSlider != null)
            gaugeSlider.value = progress;

        // 현재 스테이지 표시
        int stage = SaveSystem.Data.stage;
        levelText.text = $"Level {stage}";
        // 🔥 코인 표시 업데이트

        // ★ 국기 이미지 적용
        if (countryImage != null)
            countryImage.sprite = countrySprite;
        RefreshLobbyUI();
    }
    public void RefreshLobbyUI()
    {
        int moveLv = UpgradeManager.Instance.MoveSpeedLevel;
        int radiusLv = UpgradeManager.Instance.RadiusLevel;
        // int mutateLv = UpgradeManager.Instance.MutateChanceLevel;
        // 코인 업데이트
        coinText.text = SaveSystem.Data.coin.ToString();

        // 버튼 텍스트 업데이트
        // Move Speed UI
        if (moveLv >= 5)
            moveSpeedText.text = $"Move Speed Lv{moveLv} (MAX)";
        else
            moveSpeedText.text = $"Move Speed Lv{moveLv}";

        // Radius UI
        if (radiusLv >= 5)
            radiusText.text = $"Radius Lv{radiusLv} (MAX)";
        else
            radiusText.text = $"Radius Lv{radiusLv}";

        // 버튼 활성/비활성
        if (moveSpeedButton != null)
            moveSpeedButton.interactable = !UpgradeManager.Instance.IsMoveSpeedMax;

        if (radiusButton != null)
            radiusButton.interactable = !UpgradeManager.Instance.IsRadiusMax;
        //mutateChanceText.text = $"Mutate Chance Lv{SaveSystem.Data.mutateChanceLevel}";
    }
    private void OnClickStart()
    {
        // GameManager 통해 게임 실행
        GameManager.Instance.StartGame();
    }

    void OnUpgradeMoveSpeed()
    {
        int level = SaveSystem.Data.moveSpeedLevel;
        int cost = 10;

        if (SaveSystem.Data.coin < cost)
        {
            Debug.Log("코인이 부족함");
            return;
        }

        if (UpgradeManager.Instance.TryUpgradeMoveSpeed())
            RefreshLobbyUI();
    }

    void OnUpgradeRadius()
    {
        int level = SaveSystem.Data.radiusLevel;
        int cost = 10;

        if (SaveSystem.Data.coin < cost)
            return;

        if (UpgradeManager.Instance.TryUpgradeRadius())
            RefreshLobbyUI();
    }

    //void OnUpgradeMutate()
    //{
    //    int level = SaveSystem.Data.mutateChanceLevel;
    //    int cost = level * 10;

    //    if (SaveSystem.Data.coin < cost)
    //        return;

    //    SaveSystem.Data.coin -= cost;
    //    SaveSystem.Data.mutateChanceLevel++;
    //    SaveSystem.Save();

    //    if (UpgradeManager.Instance.TryUpgradeMutate())
    //        RefreshLobbyUI();
    //}
    public void UpdateDifficulty(LevelDifficulty diff)
{
    switch (diff)
    {
        case LevelDifficulty.Normal:
            difficultyText.text = "NORMAL";
            difficultyText.color = Color.white;

            // ⭐ Normal → Bar 끔
            if (difficultyBar != null)
                difficultyBar.SetActive(false);
            break;

        case LevelDifficulty.Hard:
            difficultyText.text = "HARD";
            difficultyText.color = new Color(0.6f, 0.3f, 1f); // 보라

            // ⭐ Hard → Bar 켬
            if (difficultyBar != null)
                difficultyBar.SetActive(true);
            break;

        case LevelDifficulty.VeryHard:
            difficultyText.text = "VERY HARD";
            difficultyText.color = Color.red;

            // ⭐ VeryHard → Bar 켬
            if (difficultyBar != null)
                difficultyBar.SetActive(true);
            break;
    }
}

}
