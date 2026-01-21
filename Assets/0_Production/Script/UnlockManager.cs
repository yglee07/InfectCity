public static class UnlockManager
{
    public static bool IsUnlocked(UnlockType type)
    {
        if (SaveSystem.Data == null)
            SaveSystem.Load();   // Data가 없으면 여기서 보장

        int stage = SaveSystem.Data.stage;

        switch (type)
        {
            case UnlockType.DragUnit: return stage >= 30;
            case UnlockType.SpeedUp: return stage >= 20;
            default: return false;
        }
    }

    public static bool DragUnitUnlocked => IsUnlocked(UnlockType.DragUnit);
    public static bool SpeedUpUnlocked => IsUnlocked(UnlockType.SpeedUp);
}
public enum UnlockType
{
    DragUnit,
    SpeedUp
}
