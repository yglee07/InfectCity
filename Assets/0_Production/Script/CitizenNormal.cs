using UnityEngine;

public class CitizenNormal : CitizenBase
{
    public enum CitizenMode { Normal, Idle, Sleep }

    [Header("Citizen Mode")]
    public CitizenMode mode = CitizenMode.Normal;

    [Header("Initial Idle Animation (One Shot)")]
    public string initialIdleAnim = ""; // 예: StickMan_Dance, StickMan_Toilet

    bool initialIdleConsumed = false;
    bool isInitialIdleActive = false;
    protected override void OnEnable()
    {
        base.OnEnable();

        initialIdleConsumed = false;

        switch (mode)
        {
            case CitizenMode.Normal:
                state = State.Wander;
                break;
            case CitizenMode.Idle:
            case CitizenMode.Sleep:
                state = State.Idle;
                break;
        }
    }

    protected override void Start()
{
    base.Start();

    if (mode == CitizenMode.Idle && !string.IsNullOrEmpty(initialIdleAnim))
    {
        // 🔒 연출용 Idle 시작
        isInitialIdleActive = true;
        initialIdleConsumed = false;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();

        PlayAnim(initialIdleAnim);

        return; // 🔥 여기서 끝 (ChangeState 안 탐)
    }

    // 일반 흐름
    switch (mode)
    {
        case CitizenMode.Normal:
            ChangeState(State.Wander);
            break;

        case CitizenMode.Idle:
            ChangeState(State.Idle);
            break;
    }
}


    protected override void Tick()
    {
         if (mode == CitizenMode.Sleep)
            return;
            
         if (isInitialIdleActive)
        {
            // 🔥 연출 중이라도 좀비는 본다
            if (DetectZombie())
            {
                isInitialIdleActive = false;
                initialIdleConsumed = true;
                ChangeState(State.Flee);
            }

            return;
        }
       

        bool detected = DetectZombie();

        // ---------------- 상태 전환 ----------------
        if (detected && state != State.Flee)
        {
            initialIdleConsumed = true; // 🔥 한 번이라도 도망치면 특수 Idle 폐기
            ChangeState(State.Flee);
        }
        else if (!detected && state == State.Flee)
        {
            ChangeState(State.Wander);
        }

        // ---------------- 상태 실행 ----------------
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

    protected override void UpdateIdle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // ⭐ 핵심: 최초 1회만 특수 Idle
        if (!initialIdleConsumed && !string.IsNullOrEmpty(initialIdleAnim))
        {
            PlayAnim(initialIdleAnim);
            initialIdleConsumed = true;
        }
        else
        {
            PlayMoveAnim("StickMan_Idle");
        }

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            ChangeState(State.Wander);
        }
    }

    protected override void DespawnSelf()
    {
        string poolKey = mode switch
        {
            CitizenMode.Idle => "Citizen_Idle",
            CitizenMode.Sleep => "Citizen_Sleep",
            _ => "Citizen_Normal"
        };

        PoolManager.Instance.Despawn(poolKey, gameObject);
    }
}
