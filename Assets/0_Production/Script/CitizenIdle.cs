using UnityEngine;

public class CitizenIdle : CitizenBase
{
    protected override void Start()
    {
        base.Start();
        ChangeState(State.Idle);   // ⭐ 시작 상태 명확히
    }

    protected override void Tick()
    {
        bool detected = DetectZombie();

        if (detected && state != State.Flee)
        {
            ChangeState(State.Flee);
        }
        else if (!detected && state == State.Flee)
        {
            ChangeState(State.Idle);
        }

        switch (state)
        {
            case State.Idle:
                UpdateIdle();
                break;

            case State.Flee:
                UpdateFlee();
                break;
        }
    }

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Idle", gameObject);
    }
}
