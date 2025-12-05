using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIGame : MonoBehaviour
{
    [Header("Stage")]
    public TMP_Text stageText;

    [Header("Infection")]
    public Slider infectionSlider;
    public TMP_Text infectionPercentText; 

    [Header("Skill")]
    public TMP_Text dragChargesText; // Drag skill charges 표시
    [Header("Popups")]
    public GameObject popupComplete;
    public GameObject popupFailed;
    [Header("Popup Buttons")]
    public Button btnCompleteOK;
    public Button btnFailedOK;
    void Awake()
    {
        if (btnCompleteOK != null)
            btnCompleteOK.onClick.AddListener(OnClickCompleteOK);

        if (btnFailedOK != null)
            btnFailedOK.onClick.AddListener(OnClickFailedOK);
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

    public void UpdateInfectionProgress(float progress)
    {
        infectionSlider.value = progress;
        int pct = Mathf.RoundToInt(progress * 100f);
        infectionPercentText.text = pct + "%";
    }

    public void UpdateCharges(int current, int max)
    {
        dragChargesText.text = $"{current}/{max}";
    }
}
