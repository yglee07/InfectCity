using UnityEngine;
using UnityEngine.AI;

public class CitizenNormal : CitizenBase
{
    protected override void Start()
    {
        base.Start();
        ChangeState(State.Idle);
    }
    protected override void Tick()
    {
       
        bool detected = DetectZombie();

       
        if (detected && state != State.Flee)
        {
     
            ChangeState(State.Flee);
        }
        else if (!detected && state != State.Wander)
        {
        
            ChangeState(State.Wander);
        }

        // ============================================
        // 상태 실행
        switch (state)
        {
            case State.Wander:
                UpdateWander();
                break;
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
        PoolManager.Instance.Despawn("Citizen_Normal", gameObject);
    }
}
