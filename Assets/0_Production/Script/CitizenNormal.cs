using UnityEngine;

public class CitizenNormal : CitizenBase
{
       public enum CitizenMode { Normal, Idle, Sleep }
    
    [Header("Citizen Mode")]
    public CitizenMode mode = CitizenMode.Normal;
    
    protected bool hasFledOnce = false;
    
    protected override void Start()
    {
        base.Start();
        
        switch (mode)
        {
            case CitizenMode.Normal:
                ChangeState(State.Wander);
                break;
                
            case CitizenMode.Idle:
                ChangeState(State.Idle);
                break;
                
            case CitizenMode.Sleep:
                if (animatedMesh == null)
                {
                    Debug.LogError("[CitizenNormal] Sleep 모드: animatedMesh is NULL");
                    return;
                }
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                PlayAnim("StickMan_Sleep");
                break;
        }
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        hasFledOnce = false;
        
        switch (mode)
        {
            case CitizenMode.Normal:
                state = State.Wander;
                break;
            case CitizenMode.Idle:
                state = State.Idle;
                break;
            case CitizenMode.Sleep:
                state = State.Idle; // Sleep은 상태가 없으므로 Idle로 설정
                break;
        }
    }
    
    protected override void Tick()
    {
        // Sleep 모드는 Tick 비활성화
        if (mode == CitizenMode.Sleep)
        {
            return;
        }
        
        bool detected = DetectZombie();

        // =========================
        // 상태 전환
        // =========================
        if (mode == CitizenMode.Idle)
        {
            // Idle 모드: CitizenIdle 로직
            if (detected && state != State.Flee)
            {
                ChangeState(State.Flee);
                hasFledOnce = true;
            }
            else if (!detected && state == State.Flee)
            {
                ChangeState(State.Idle);
            }
        }
        else
        {
            // Normal 모드: 기존 로직
            if (detected && state != State.Flee)
            {
                ChangeState(State.Flee);
            }
            else if (!detected && state == State.Flee)
            {
                ChangeState(State.Wander);
            }
        }

        // =========================
        // 상태 실행
        // =========================
        switch (state)
        {
            case State.Idle:
                if (mode == CitizenMode.Idle && hasFledOnce)
                {
                    UpdateIdle();
                }
                else if (mode == CitizenMode.Normal)
                {
                    UpdateIdle();
                }
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
        // 모드에 따라 다른 키 사용 (프리팹 호환성)
        string poolKey = mode switch
        {
            CitizenMode.Idle => "Citizen_Idle",
            CitizenMode.Sleep => "Citizen_Sleep",
            _ => "Citizen_Normal"
        };
        PoolManager.Instance.Despawn(poolKey, gameObject);
    }
}
