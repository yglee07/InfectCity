#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ResetSaveMenu
{
    [MenuItem("저장 데이터 초기화/초기화하기")]
    public static void ResetSave()
    {
        if (!EditorUtility.DisplayDialog(
            "저장 데이터 초기화",
            "데이터를 초기화하고\n1스테이지로 돌아갑니다.\n\n계속하시겠습니까?",
            "초기화",
            "취소"
        ))
        {
            return;
        }

        string path = Path.Combine(
            Application.persistentDataPath,
            "SaveData.json"
        );

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[ResetSaveMenu] SaveData.json 삭제 완료");
        }
        else
        {
            Debug.Log("[ResetSaveMenu] 삭제할 SaveData.json이 없습니다.");
        }
    }
}
#endif
