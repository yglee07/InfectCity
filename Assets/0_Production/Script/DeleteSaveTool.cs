using UnityEngine;
using System.IO;

public class DeleteSaveTool : MonoBehaviour
{
    [ContextMenu("Delete SaveData.json")]
    public void DeleteSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "SaveData.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[DeleteSaveTool] SaveData.json 삭제 완료\n{path}");
        }
        else
        {
            Debug.Log("[DeleteSaveTool] 삭제할 SaveData.json이 없습니다.");
        }
    }
}
