using UnityEngine;

[System.Serializable]
public class CountryData
{
    public string id;
    public string displayName;
    public int startStage;
    public int endStage;
    public GameObject prefab;

    public int TotalStages => endStage - startStage + 1;
}
