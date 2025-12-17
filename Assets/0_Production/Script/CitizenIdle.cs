public class CitizenIdle : CitizenBase
{
    protected override void Start()
    {
        base.Start();
        ChangeState(State.Idle);   // ⭐ 시작만 Idle
    }

    protected override void Tick()
    {
        bool detected = DetectZombie();
     

        if (detected)
        {
            if (state != State.Flee)
                ChangeState(State.Flee);

            UpdateFlee();
        }
        // ❌ else에서 Idle/Wander 전환 안 함
        // → Flee 끝나면 Base에서 Wander로 처리
    }

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Idle", gameObject);
    }
}
