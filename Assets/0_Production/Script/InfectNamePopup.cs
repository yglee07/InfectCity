using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfectorNamePopup : MonoBehaviour
{
    public UIGame hud;  // ← 여기 public reference
    [Header("UI References")]
    public TMP_InputField inputField;
    public Button playButton;
    public Button cancelButton;
    public TextMeshProUGUI warningText;

    private void Start()
    {
        warningText.gameObject.SetActive(false);

        playButton.onClick.AddListener(OnClickPlay);
        cancelButton.onClick.AddListener(OnClickCancel);
    }

    void OnClickPlay()
    {
        string name = inputField.text.Trim();

        // 특수문자 금지 (영문/숫자/공백만 허용)
        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9 ]+$"))
        {
            warningText.gameObject.SetActive(true);
            warningText.text = "Special characters are not allowed.";
            return;
        }

        // 공백이면 기본 이름 사용
        if (string.IsNullOrEmpty(name))
            name = "Unknown Infector";

        // 저장
        InfectorName.Current = name;
        
        
        // HUD에 직접 반영
        if (hud != null)
            hud.UpdateInfectorName(name);
        Debug.Log("Infector Name: " + InfectorName.Current);

        // 팝업 닫기
        this.gameObject.SetActive(false);

        // 이후 게임 시작 로직 넣으면 됨
        // GameManager.Instance.StartGame();
    }

    void OnClickCancel()
    {
        this.gameObject.SetActive(false);
    }
}
public static class InfectorName
{
    public static string Current = "";
}