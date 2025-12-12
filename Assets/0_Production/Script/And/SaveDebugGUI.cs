using UnityEngine;

public class SaveDebugGUI : MonoBehaviour
{
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.normal.textColor = Color.white;

        GUILayout.BeginArea(new Rect(20, 20, 400, 300));
        GUILayout.Label("<b>SAVE DATA DEBUG</b>", style);

        if (SaveSystem.Data != null)
        {
            GUILayout.Label($"Stage : {SaveSystem.Data.stage}", style);
            GUILayout.Label($"Coin  : {SaveSystem.Data.coin}", style);
            GUILayout.Label($"Gem   : {SaveSystem.Data.gem}", style);
        }
        else
        {
            GUILayout.Label("SaveData = NULL (Load 실패?)", style);
        }

        GUILayout.Space(10);

        // 수동 저장 버튼
        if (GUILayout.Button("Save", GUILayout.Height(40)))
        {
            SaveSystem.Save();
            Debug.Log("Manual Save Complete!");
        }

        // 초기화 버튼
        if (GUILayout.Button("Reset SaveData", GUILayout.Height(40)))
        {
            SaveSystem.Reset();
            Debug.Log("Reset SaveData!");
        }

        GUILayout.EndArea();
    }
}
