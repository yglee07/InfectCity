using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int stage = 1;
    public int coin = 0;
    public int gem = 0;

    public int moveSpeedLevel = 1;
    public int radiusLevel = 1;
    public int mutateChanceLevel = 1;

    // ⭐ Dictionary → List
    public List<CountryStageEntry> countryStages = new();

    public int GetClearedStageCount(string countryId)
    {
        var entry = countryStages.Find(e => e.countryId == countryId);
        return entry != null ? entry.cleared : 0;
    }

    public void AddClearedStage(string countryId)
    {
        var entry = countryStages.Find(e => e.countryId == countryId);

        if (entry != null)
        {
            entry.cleared++;
        }
        else
        {
            countryStages.Add(new CountryStageEntry
            {
                countryId = countryId,
                cleared = 1
            });
        }
    }
}
[System.Serializable]
public class CountryStageEntry
{
    public string countryId;
    public int cleared;
}
