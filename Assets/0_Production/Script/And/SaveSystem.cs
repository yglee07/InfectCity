using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string filePath => Path.Combine(Application.persistentDataPath, "SaveData.json");

    public static SaveData Data { get; private set; }

    // 게임 시작할 때 자동으로 꼭 호출해야 함
    public static void Load()
    {
        if (!File.Exists(filePath))
        {
            // 파일 없으면 새로 생성
            Data = new SaveData();
            Save();
            return;
        }

        string json = File.ReadAllText(filePath);
        Data = JsonUtility.FromJson<SaveData>(json);
    }

    public static void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(filePath, json);
        Debug.Log("Saved: " + filePath);
    }

    public static void Reset()
    {
        Data = new SaveData();
        Save();
    }
}
