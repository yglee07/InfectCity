using UnityEngine;

public class GameManager : MonoBehaviour
{
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 36;               // 글씨 크게
        style.fontStyle = FontStyle.Bold;  // 볼드체
        style.normal.textColor = Color.white;

        // 간단한 그림자 효과 (두 번째 텍스트를 어둡게 해서 약간 뒤에 출력)
        GUIStyle shadow = new GUIStyle(style);
        shadow.normal.textColor = new Color(0, 0, 0, 0.6f);

        float x = 30f;
        float y = 30f;

        // 그림자 먼저
        GUI.Label(new Rect(x + 2, y + 2, 900, 60), "▶ 1번을 누르면 Level1이 생성됩니다.", shadow);
        GUI.Label(new Rect(x + 2, y + 52, 900, 60), "▶ 화면을 드래그하면 범위 내 시민들이 감염됩니다.", shadow);

        // 본문 텍스트
        GUI.Label(new Rect(x, y, 900, 60), "▶ 1번을 누르면 Level1이 생성됩니다.", style);
        GUI.Label(new Rect(x, y + 50, 900, 60), "▶ 화면을 드래그하면 범위 내 시민들이 감염됩니다.", style);
    }
}
