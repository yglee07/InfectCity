using UnityEngine;
using TMPro;

public class UINewsTicker : MonoBehaviour
{
    public TMP_Text text;
    public float speed = 120f;
    public float extraPadding = 20f;

    RectTransform rt;
    RectTransform parentRT;
    float endX;

    void Awake()
    {
        rt = text.rectTransform;
        parentRT = rt.parent as RectTransform;
    }

    void OnEnable()
    {
        ResetPosition();
    }

    void Update()
    {
        Vector2 pos = rt.anchoredPosition;
        pos.x -= speed * Time.deltaTime;
        rt.anchoredPosition = pos;

        // 🔥 텍스트가 화면 왼쪽 완전히 벗어난 뒤
        if (pos.x <= endX)
        {
            ResetPosition();
        }
    }

    void ResetPosition()
    {
        // 시작: 오른쪽 끝
        rt.anchoredPosition =
            new Vector2(0f, rt.anchoredPosition.y);

        // 종료: 텍스트 길이 + 부모 너비 + 여유
        endX = -(text.preferredWidth + parentRT.rect.width + extraPadding);
    }

    public void SetText(string msg)
    {
        text.text = msg;
        ResetPosition();
    }
}
