using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIGame : MonoBehaviour
{
    [Header("Stage")]
    public TMP_Text stageText;

    [Header("Skill")]
    public TMP_Text dragChargesText; // Drag skill charges 표시
    [Header("Popups")]
    public GameObject popupComplete;
    public GameObject popupFailed;
    [Header("Popup Buttons")]
    public Button btnCompleteOK;
    public Button btnFailedOK;
    [Header("Mode Buttons")]
    public Button btnInfectMode;
    public Button btnCameraMode;

    [Header("Pie Chart")]
    public Image[] imagesPieChart;
    public TMP_Text pieCenterText;
    public float pieSmoothSpeed = 6f;

    private float[] pieValues = new float[4]; // purple, yellow, citizen, green

    [SerializeField] Button speedToggleButton;
    [SerializeField] TMP_Text speedText;

     public Button bombButton;
    public Button unitButton;
     public void RefreshActionButtons(
        int bombCharges,
        int unitCharges
    )
    {
        if (bombButton != null)
            bombButton.interactable = bombCharges > 0;

        if (unitButton != null)
            unitButton.interactable = unitCharges > 0;
    }
    void Awake()
    {
        if (btnCompleteOK != null)
            btnCompleteOK.onClick.AddListener(OnClickCompleteOK);

        if (btnFailedOK != null)
            btnFailedOK.onClick.AddListener(OnClickFailedOK);
        if(speedToggleButton!=null)
        {
            speedToggleButton.onClick.AddListener(OnClickToggleSpeed);
        }
      
    }

    private void Update()
    {
        UpdatePieChart();
    }
    public void ShowCompletePopup()
    {
        popupComplete.SetActive(true);
    }

    public void ShowFailedPopup()
    {
        popupFailed.SetActive(true);
    }

    public void OnClickCompleteOK()
    {
        

        popupComplete.SetActive(false);

        GameManager.Instance.ReturnToLobby();
    }

    public void OnClickFailedOK()
    {
        popupFailed.SetActive(false);

        GameManager.Instance.ReturnToLobby();
    }
    public void SetStage(int stage)
    {
        stageText.text = $"Stage {stage}";
    }

    public void UpdateCharges(int current, int max)
    {
        dragChargesText.text = $"Drag Me\n{current}/{max}";
    }
   void OnClickToggleSpeed()
    {
        Game.Instance.ToggleSpeed();
        UpdateLabel();
    }
      void UpdateLabel()
    {
        if (speedText != null)
            speedText.text = $"{Game.Instance.GameSpeed:0.#}x";
    }
    public void UpdatePieChart()
    {
        // 1) 값 수집
        float blue = NPCManager.Instance.Citizens.Count;
        float green = NPCManager.Instance.GreenZombies.Count;
        float purple = NPCManager.Instance.PurpleZombies.Count;
        float yellow = NPCManager.Instance.YellowZombies.Count;

        pieValues[0] = purple;
        pieValues[1] = yellow;
        pieValues[2] = blue;
        pieValues[3] = green;

        // 2) UI 적용
        SetPieValues(pieValues);
        UpdatePieText(pieValues);
    }

    private void SetPieValues(float[] values)
    {
        float total = values[0] + values[1] + values[2] + values[3];
        if (total <= 0) total = 1;

        float startAngle = 0f; // 시작 각도 (누적, 0~1 범위)

        for (int i = 0; i < imagesPieChart.Length && i < values.Length; i++)
        {
            float percent = values[i] / total;
            
            // 각 슬라이스의 fillAmount는 해당 슬라이스의 비율만큼만 설정
            float targetFillAmount = percent;
            
            imagesPieChart[i].fillAmount = Mathf.Lerp(
                imagesPieChart[i].fillAmount,
                targetFillAmount,
                Time.deltaTime * pieSmoothSpeed
            );
            
            // 각 슬라이스 Image를 올바른 시작 각도로 부드럽게 회전
            RectTransform rectTransform = imagesPieChart[i].rectTransform;
            float targetRotation = startAngle * 360f;
            float currentRotation = rectTransform.localEulerAngles.z;
            
            // 각도 보간 (360도 경계 처리)
            float smoothRotation = Mathf.LerpAngle(currentRotation, -targetRotation, Time.deltaTime * pieSmoothSpeed);
            rectTransform.localRotation = Quaternion.Euler(0, 0, smoothRotation);
            
            // fillOrigin은 0 (Bottom)으로 설정 (회전으로 각도 조정)
            imagesPieChart[i].fillOrigin = 0;
            
            // 다음 슬라이스의 시작 각도 업데이트
            startAngle += percent;
        }
    }

    private void UpdatePieText(float[] values)
    {
        // total = purple + yellow + citizen + green
        float total = values[0] + values[1] + values[2] + values[3];
        if (total <= 0)
        {
            pieCenterText.text = "0%";
            return;
        }

        float greenRatio = values[3] / total; // values[3] = green
        int pct = Mathf.RoundToInt(greenRatio * 100f);

        pieCenterText.text = pct + "%";
    }
}
