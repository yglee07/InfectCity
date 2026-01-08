using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UINamePopup : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;
    public Button confirmButton;

    void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClickConfirm);
    }

    void OnClickConfirm()
    {
        string name = inputField.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("Name is empty");
            return;
        }

        SaveSystem.Data.infectorName = name;
        SaveSystem.Save();

        gameObject.SetActive(false);

        Debug.Log($"[UINamePopup] Infector name confirmed: {name}");
    }
}
