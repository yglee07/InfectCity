using UnityEngine;
public class CitizenIdle : CitizenBase
{
    protected override void Tick()
    {
        bool detected = DetectZombie();

        if (detected)
            ChangeState(State.Flee);
        else
            ChangeState(State.Idle);

        if (state == State.Flee)
            UpdateFlee();
    }

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Idle", gameObject);
    }
}