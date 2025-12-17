using UnityEngine;
using UnityEngine.AI;
public class CitizenSleep : CitizenBase
{
    protected override void Awake()
    {
        base.Awake();

        // 🔥 행동 로직 차단
        //agent.enabled = false;

        // 🔥 애니메이션 고정
        PlayAnim("Stickman_Sleep");
    }

    protected override void Start()
    {
        
        // 🔹 "행동 시작"은 Start에서
        if (!agent.enabled)
            agent.enabled = true;

    }

    protected override void Tick()
    {
        // 아무것도 안 함
    }

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Sleep", gameObject);
    }
}
