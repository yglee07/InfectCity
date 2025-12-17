using UnityEngine;
using UnityEngine.AI;
public class CitizenSleep : CitizenBase
{
    
  


    protected override void Start()
    {
        base.Start();

        // 이 시점이면 animatedMesh + SO + Disable 처리 다 끝남
        if (animatedMesh == null)
        {
            Debug.LogError("[CitizenSleep] animatedMesh is NULL");
            return;
        }

        PlayAnim("StickMan_Sleep");
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
