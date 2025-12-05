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
