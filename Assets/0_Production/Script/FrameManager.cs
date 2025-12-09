using UnityEngine;

public class FrameManager : MonoBehaviour
{
    void Awake()
    {
        // 초당 최대 프레임 제한
        Application.targetFrameRate = 60;

        // 모바일에서도 VSync 무시하고 60프레임 유지
        QualitySettings.vSyncCount = 0;
    }
}