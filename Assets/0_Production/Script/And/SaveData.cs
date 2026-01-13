using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public string infectorName;
    public int stage = 1;
    public int coin = 0;
    public int gem = 0;

    public int moveSpeedLevel = 1;
    public int radiusLevel = 1;
    public int mutateChanceLevel = 1;
    public float lastGameSpeed = 1f;
    // ⭐ Dictionary → List
    public List<CountryStageEntry> countryStages = new();
    public bool hasPendingConquerAnim;
    public string pendingCountryId;
    public int pendingBeforeCleared;
    public int pendingAfterCleared;
    public int pendingGreenZombieCount;

    public List<int> tutorialClearedStages = new List<int>();


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
    public bool IsTutorialCleared(int stage)
    {
        return tutorialClearedStages.Contains(stage);
    }

    public void MarkTutorialCleared(int stage)
    {
        if (!tutorialClearedStages.Contains(stage))
            tutorialClearedStages.Add(stage);
    }
}
[System.Serializable]
public class CountryStageEntry
{
    public string countryId;
    public int cleared;
}
