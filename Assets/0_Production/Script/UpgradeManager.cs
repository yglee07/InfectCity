using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // -----------------------
    // 레벨 불러오기
    // -----------------------
    public int MoveSpeedLevel => SaveSystem.Data.moveSpeedLevel;
    public int RadiusLevel => SaveSystem.Data.radiusLevel;
    public int MutateChanceLevel => SaveSystem.Data.mutateChanceLevel;

    // -----------------------
    // 강화 수치 변환
    // -----------------------

    // 예: 속도 레벨당 +10%
    public float GetMoveSpeedMultiplier()
    {
        return 1f + (MoveSpeedLevel - 1) * 0.1f;
    }

    // Radius 레벨당 +20%
    public float GetBombRadiusMultiplier()
    {
        return 1f + (RadiusLevel - 1) * 0.2f;
    }

    // 변이 확률 레벨당 +3%
    public float GetMutateChance()
    {
        return 0.05f + (MutateChanceLevel - 1) * 0.03f;
    }

    // -----------------------
    // 코인 차감 후 강화 레벨 상승
    // -----------------------
    public bool TryUpgradeMoveSpeed()
    {
        int cost = 10;
        if (SaveSystem.Data.coin < cost) return false;

        SaveSystem.Data.coin -= cost;
        SaveSystem.Data.moveSpeedLevel++;
        SaveSystem.Save();
        return true;
    }

    public bool TryUpgradeRadius()
    {
        int cost = 10;
        if (SaveSystem.Data.coin < cost) return false;

        SaveSystem.Data.coin -= cost;
        SaveSystem.Data.radiusLevel++;
        SaveSystem.Save();
        return true;
    }

    public bool TryUpgradeMutate()
    {
        int cost = 10;
        if (SaveSystem.Data.coin < cost) return false;

        SaveSystem.Data.coin -= cost;
        SaveSystem.Data.mutateChanceLevel++;
        SaveSystem.Save();
        return true;
    }
}
