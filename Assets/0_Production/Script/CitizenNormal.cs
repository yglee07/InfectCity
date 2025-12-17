using UnityEngine;

public class CitizenNormal : CitizenBase
{
    protected override void Start()
    {
        base.Start();
        ChangeState(State.Wander); // ✅ 안전
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        // 상태만 변수로 지정
        state = State.Wander;
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
        else if (!detected && state == State.Flee)
        {
            ChangeState(State.Wander);
        }

        // =========================
        // 상태 실행
        // =========================
        switch (state)
        {
            case State.Idle:
                UpdateIdle();
                break;

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
