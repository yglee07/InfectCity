using UnityEngine;
public class CitizenIdle : CitizenBase
{
    protected override void Tick()
    {

        bool detected = DetectZombie();

        // ============================================
        // ★ CitizenBehaviorType.Idle 전용 로직
        // ============================================

        if (!detected)
        {
            // 좀비 없으면 Idle 유지
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            PlayAnim("Idle");


            return;   // Wander/Idle 랜덤 로직 접근 금지
        }
        else
        {

            agent.isStopped = false;
            // 좀비 감지 → Flee 전환
            if (state != State.Flee)
            {
                ChangeState(State.Flee);

            }
        }


        // ============================================
        // Normal 시민 + Idle 시민 공통 상태 전환
        // ============================================
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
        // ============================================
        if (state == State.Wander)
        {

            UpdateWander();
        }
        else
        {
            UpdateFlee();
        }
    }

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Idle", gameObject);
    }
}