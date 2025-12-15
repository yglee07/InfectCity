using UnityEngine;
using UnityEngine.AI;

public class CitizenNormal : CitizenBase
{
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
}
