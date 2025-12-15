using UnityEngine;
using UnityEngine.AI;

public class CitizenSleep : CitizenBase
{
    protected override void Tick()
    {
        if (!agent.enabled) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        PlayAnim("Sleep");
    }
}
