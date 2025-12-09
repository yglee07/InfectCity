using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    private const int MaxLevel = 5;   // ← 업그레이드 최대 레벨

    void Awake()
    {
        Instance = this;
    }

    // -----------------------
    // 레벨 불러오기
    // -----------------------
    public int MoveSpeedLevel => Mathf.Min(SaveSystem.Data.moveSpeedLevel, MaxLevel);
    public int RadiusLevel => Mathf.Min(SaveSystem.Data.radiusLevel, MaxLevel);
    public int MutateChanceLevel => Mathf.Min(SaveSystem.Data.mutateChanceLevel, MaxLevel);

    // Max 여부 체크용 헬퍼
    public bool IsMoveSpeedMax => MoveSpeedLevel >= MaxLevel;
    public bool IsRadiusMax => RadiusLevel >= MaxLevel;
    public bool IsMutateMax => MutateChanceLevel >= MaxLevel;

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
        if (IsMoveSpeedMax)
        {
            Debug.Log("MoveSpeed 이미 최대 레벨입니다.");
            return false;
        }

        int cost = 10;
        if (SaveSystem.Data.coin < cost) return false;

        SaveSystem.Data.coin -= cost;
        SaveSystem.Data.moveSpeedLevel++;
        SaveSystem.Save();
        return true;
    }

    public bool TryUpgradeRadius()
    {
        if (IsRadiusMax)
        {
            Debug.Log("Radius 이미 최대 레벨입니다.");
            return false;
        }

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
