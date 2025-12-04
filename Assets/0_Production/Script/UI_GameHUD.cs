using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TopBarHUD : MonoBehaviour
{
    [Header("UI Text")]
    public TMP_Text infectorNameText;
    public TMP_Text citizenCountText;
    public TMP_Text zombieCountText;
    public TMP_Text infectionPercentText;

    [Header("Gauge")]
    public Image infectionGaugeFill; // Image(fill type) 사용

    private float updateInterval = 0.2f;
    private float timer;

    public void UpdateInfectorName(string newName)
    {
        infectorNameText.text = $"{newName}";
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > updateInterval)
        {
            timer = 0f;
            UpdateHUD();
        }
    }

    void UpdateHUD()
    {
        int citizen = NPCManager.Instance.Citizens.Count;
        int zombie = NPCManager.Instance.Zombies.Count;

        int total = citizen + zombie;
        float percent = (total > 0) ? (float)zombie / total : 0f;

        citizenCountText.text = $"Citizens: {citizen}";
        zombieCountText.text = $"Infected: {zombie}";
        infectionPercentText.text = $"{Mathf.RoundToInt(percent * 100f)}%";

        infectionGaugeFill.fillAmount = percent;

        UpdateGaugeColor(percent);
    }

    // 감염률에 따라 게이지 색상 변화 (선택)
    void UpdateGaugeColor(float p)
    {
        if (p < 0.3f) infectionGaugeFill.color = Color.green;
        else if (p < 0.7f) infectionGaugeFill.color = Color.yellow;
        else infectionGaugeFill.color = Color.red;
    }
}
