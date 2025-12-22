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
    void Awake()
    {
        if (btnCompleteOK != null)
            btnCompleteOK.onClick.AddListener(OnClickCompleteOK);

        if (btnFailedOK != null)
            btnFailedOK.onClick.AddListener(OnClickFailedOK);

      
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
        SaveSystem.Data.stage++;
        SaveSystem.Data.coin += 10;
        SaveSystem.Save();

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

    float accumulated = 0f;

    for (int i = 0; i < imagesPieChart.Length && i < values.Length; i++)
    {
        float percent = values[i] / total;
        accumulated += percent;

        // ⭐ 누적 fillAmount 사용
        imagesPieChart[i].fillAmount = Mathf.Lerp(
            imagesPieChart[i].fillAmount,
            accumulated,
            Time.deltaTime * pieSmoothSpeed
        );

        // ⭐ 회전 필요 없음
        imagesPieChart[i].rectTransform.localRotation = Quaternion.identity;
        imagesPieChart[i].fillOrigin = 0;
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
