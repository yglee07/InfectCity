using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int stage = 1;
       public Dictionary<string, int> countryStageCount
        = new Dictionary<string, int>();
    public int coin = 0;
    public int gem = 0;

    public int moveSpeedLevel = 1;
    public int radiusLevel = 1;
    public int mutateChanceLevel = 1;

    public int GetClearedStageCount(string countryId)
    {
        if (countryStageCount == null)
            countryStageCount = new Dictionary<string, int>();

        if (countryStageCount.TryGetValue(countryId, out int v))
            return v;

        return 0;
    }

    public void AddClearedStage(string countryId)
    {
        if (countryStageCount == null)
            countryStageCount = new Dictionary<string, int>();

        if (!countryStageCount.ContainsKey(countryId))
            countryStageCount[countryId] = 0;

        countryStageCount[countryId]++;
    }
}