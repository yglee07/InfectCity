using UnityEngine;
using TMPro;

public class UITutorial : MonoBehaviour
{
    [Header("Unit Intro")]
    [SerializeField] GameObject unitNameRoot;
    [SerializeField] TMP_Text unitNameText;

    [SerializeField] GameObject unitDescRoot;
    [SerializeField] TMP_Text unitDescText;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowUnitIntro(string name, string desc)
    {
        gameObject.SetActive(true);

        // 이름
        if (string.IsNullOrEmpty(name))
        {
            unitNameRoot.SetActive(false);
        }
        else
        {
            unitNameText.text = name;
            unitNameRoot.SetActive(true);
        }

        // 설명
        if (string.IsNullOrEmpty(desc))
        {
            unitDescRoot.SetActive(false);
        }
        else
        {
            unitDescText.text = desc;
            unitDescRoot.SetActive(true);
        }
    }

    public void HideUnitIntro()
    {
        unitNameRoot.SetActive(false);
        unitDescRoot.SetActive(false);
        gameObject.SetActive(false);
    }
}
