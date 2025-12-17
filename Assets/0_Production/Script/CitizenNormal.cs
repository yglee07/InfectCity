using UnityEngine;

public class CitizenNormal : CitizenBase
{
    protected override void Start()
    {
        base.Start();
        ChangeState(State.Wander); // ⭐ 처음부터 Wander
    }

    protected override void Tick()
    {
        bool detected = DetectZombie();
     

        // =========================
        // 상태 전환
        // =========================
        if (detected && state != State.Flee)
        {
            ChangeState(State.Flee);
        }

        // =========================
        // 상태 실행
        // =========================
        switch (state)
        {
            case State.Wander:
                UpdateWander();
                break;

            case State.Flee:
                UpdateFlee();
                break;
        }
    }

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Normal", gameObject);
    }
}
