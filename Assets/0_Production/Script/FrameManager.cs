using UnityEngine;

public class FrameManager : MonoBehaviour
{
    [Header("FPS Display")]
    public bool showFPS = true;
    public float fpsUpdateInterval = 0.5f;

    private float fpsTimer;
    private int frameCount;
    private float currentFPS;

    void Awake()
    {
        // 초당 최대 프레임 제한
        Application.targetFrameRate = 60;

        // VSync 끄기
        QualitySettings.vSyncCount = 0;
    }

    void Update()
    {
        if (!showFPS) return;

        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= fpsUpdateInterval)
        {
            currentFPS = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!showFPS) return;

        GUI.color = Color.white;
        GUI.Label(
            new Rect(10, 10, 150, 30),
            $"FPS: {currentFPS:F1}"
        );
    }
}
