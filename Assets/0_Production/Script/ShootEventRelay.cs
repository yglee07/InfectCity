using UnityEngine;

public class ShootEventRelay : MonoBehaviour
{
    public CitizenShooter shooter;

    // Animation Event에서 호출됨
    public void OnShootEvent()
    {
        if (shooter != null)
            shooter.OnShootEvent();
    }
}
