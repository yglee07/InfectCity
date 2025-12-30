using UnityEngine;
using UnityEngine.UI;

public class UnlockableButton : MonoBehaviour
{
    public UnlockType unlockType;

    [Header("Lock UI")]
    public GameObject txtLock;
    public GameObject imgLock;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        bool unlocked = UnlockManager.IsUnlocked(unlockType);

        if (txtLock != null)
            txtLock.SetActive(!unlocked);

        if (imgLock != null)
            imgLock.SetActive(!unlocked);

        if (button != null)
            button.interactable = unlocked;
    }
}
