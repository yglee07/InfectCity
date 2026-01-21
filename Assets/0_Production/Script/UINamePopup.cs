using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
public class UINamePopup : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;
    public Button confirmButton;
    public TMP_Text warningText;
    void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);

        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    void OnClickConfirm()
    {
        string name = inputField.text.Trim();

        // 경고 초기화
        if (warningText != null)
            warningText.gameObject.SetActive(false);

        if (name.Length < 2 || name.Length > 10)
        {
            ShowWarning("Name must be 2~10 characters");
            return;
        }

        if (!Regex.IsMatch(name, @"^[a-zA-Z0-9]+$"))
        {
            ShowWarning("Only letters and numbers allowed");
            return;
        }

        SaveSystem.Data.infectorName = name;
        SaveSystem.Save();

        gameObject.SetActive(false);
    }
    void ShowWarning(string msg)
    {
        if (warningText == null) return;

        warningText.text = msg;
        warningText.gameObject.SetActive(true);
    }
}
