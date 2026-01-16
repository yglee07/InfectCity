#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class SaveDataManagerWindow : EditorWindow
{
    int targetStage = 1;

    [MenuItem("저장 데이터 관리/저장 데이터 관리자")]
    static void Open()
    {
        GetWindow<SaveDataManagerWindow>("SaveData Manager");
    }

    void OnGUI()
    {
        GUILayout.Label("SaveData 관리", EditorStyles.boldLabel);
        GUILayout.Space(10);

        DrawCurrentInfo();
        GUILayout.Space(15);

        DrawStageJump();
        GUILayout.Space(20);

        DrawReset();
    }

    void DrawCurrentInfo()
    {
        if (!File.Exists(GetSavePath()))
        {
            EditorGUILayout.HelpBox("SaveData.json 없음", MessageType.Warning);
            return;
        }

        SaveSystem.Load();
        EditorGUILayout.LabelField("현재 스테이지", SaveSystem.Data.stage.ToString());
    }

    void DrawStageJump()
    {
        GUILayout.Label("스테이지 이동 (데이터 유지)", EditorStyles.boldLabel);

        targetStage = EditorGUILayout.IntField("Target Stage", targetStage);
        targetStage = Mathf.Max(1, targetStage);

        if (GUILayout.Button("이 스테이지로 이동"))
        {
            if (!EditorUtility.DisplayDialog(
                "스테이지 이동",
                $"스테이지를 {targetStage}로 변경합니다.\n(데이터는 유지됩니다)",
                "이동",
                "취소"
            ))
                return;

            SaveSystem.Load();
            SaveSystem.Data.stage = targetStage;
            SaveSystem.Save();

            Debug.Log($"[SaveDataManager] Stage → {targetStage}");
        }

        if (GUILayout.Button("Stage 기준으로 국가 진행도 재계산"))
        {
            if (!EditorUtility.DisplayDialog(
                "국가 진행도 재계산",
                $"Stage {SaveSystem.Data.stage} 기준으로\ncountry cleared 값을 자동 재계산합니다.",
                "재계산",
                "취소"
            ))
                return;

            RecalculateCountryProgress();
        }
    }

    void DrawReset()
    {
        GUILayout.Label("전체 초기화", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "SaveData.json을 완전히 삭제합니다.\n(스테이지, 업그레이드, 국가 진행도 전부 초기화)",
            MessageType.Error
        );

        if (GUILayout.Button("⚠ 전체 초기화 (1스테이지)"))
        {
            if (!EditorUtility.DisplayDialog(
                "저장 데이터 초기화",
                "모든 저장 데이터를 삭제하고 1스테이지로 돌아갑니다.\n\n진짜로 하시겠습니까?",
                "초기화",
                "취소"
            ))
                return;

            string path = GetSavePath();
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[SaveDataManager] SaveData.json 삭제 완료");
            }
            else
            {
                Debug.Log("[SaveDataManager] 삭제할 SaveData.json 없음");
            }
        }
    }

    string GetSavePath()
    {
        return Path.Combine(
            Application.persistentDataPath,
            "SaveData.json"
        );
    }

    void RecalculateCountryProgress()
    {
        SaveSystem.Load();

        Lobby lobby = FindObjectOfType<Lobby>();
        if (lobby == null)
        {
            Debug.LogError("[SaveDataManager] Lobby not found");
            return;
        }

        int stage = SaveSystem.Data.stage;
        int remaining = stage;

        // 🔥 기존 cleared 전부 제거
        SaveSystem.Data.countryStages.Clear();

        var order = lobby.GetCountryProgressOrder();

        foreach (var node in order)
        {
            if (node == null) continue;

            int cleared = Mathf.Min(
                node.stagesToConquer,
                remaining
            );

            if (cleared > 0)
            {
                SaveSystem.Data.countryStages.Add(
                    new CountryStageEntry
                    {
                        countryId = node.countryId,
                        cleared = cleared
                    }
                );
            }

            remaining -= cleared;
            if (remaining <= 0)
                break;
        }
    

        // 🔥 연출 관련 데이터 정리 (꼭 필요)
        SaveSystem.Data.hasPendingConquerAnim = false;
        SaveSystem.Data.pendingCountryId = null;
        SaveSystem.Data.pendingBeforeCleared = 0;
        SaveSystem.Data.pendingAfterCleared = 0;
        SaveSystem.Data.pendingGreenZombieCount = 0;

        SaveSystem.Save();

        Debug.Log("[SaveDataManager] Country cleared recalculated from stage");
    }

}
#endif
