using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILobby : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text countryNameText;   // 국가명
    public TMP_Text percentText;       // 퍼센트
    public TMP_Text levelText;         // ← 추가: "Level 999"

    [Header("Gauge")]
    public Slider gaugeSlider;

    public void UpdateCountryUI(string countryName, float progress)
    {
        // 국가명
        countryNameText.text = countryName;

        // 퍼센트
        int pct = Mathf.RoundToInt(progress * 100f);
        percentText.text = pct + "%";

        // 게이지
        if (gaugeSlider != null)
            gaugeSlider.value = progress;

        // 현재 레벨 표시
        int stage = SaveSystem.Data.stage;
        levelText.text = $"Level {stage}";
    }
}
