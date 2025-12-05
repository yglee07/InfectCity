using UnityEngine;

[CreateAssetMenu(fileName = "CountryDatabase", menuName = "Game/Country Database")]
public class CountryDatabase : ScriptableObject
{
    public CountryData[] countries;

    public CountryData GetCountryByStage(int stage)
    {
        foreach (var c in countries)
        {
            if (stage >= c.startStage && stage <= c.endStage)
                return c;
        }
        return null;
    }
}
