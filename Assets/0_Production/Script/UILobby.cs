using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILobby : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text countryNameText;   // 국가명
    public TMP_Text percentText;       // 퍼센트
    public TMP_Text levelText;         // "Level 999"

    [Header("Gauge")]
    public Slider gaugeSlider;

    [Header("Buttons")]
    public Button startButton;         // ← Start 버튼

    void Start()
    {
        // 버튼 클릭 시 StartGame 호출
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnClickStart);
        }
        else
        {
            Debug.LogError("UILobby: startButton이 연결되지 않았습니다!");
        }
    }

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

        // 현재 스테이지 표시
        int stage = SaveSystem.Data.stage;
        levelText.text = $"Level {stage}";
    }

    private void OnClickStart()
    {
        // GameManager 통해 게임 실행
        GameManager.Instance.StartGame();
    }
}
