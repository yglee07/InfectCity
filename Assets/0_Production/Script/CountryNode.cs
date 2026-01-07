using System.Collections;
using UnityEngine;

public class CountryNode : MonoBehaviour
{
    [Header("Identity")]
    public string countryId;
    public Transform center;
    public MeshRenderer countryMesh;

    [Header("Conquer Settings")]
    public int stagesToConquer = 3;

    [Header("Colors")]
    public Color baseColor = Color.white;
    public Color conqueredColor = new Color(0.3f, 0.8f, 0.3f, 1f);

    [Header("Step Animation")]
    public int conquerStepCount = 5;
    public float stepLerpDuration = 0.15f;

    Material mat;
    Coroutine stepRoutine;

    int currentStep;
    int totalSteps;

    Color startColor;
    Color targetColor;
    public System.Action OnConquerAnimationFinished;
    [HideInInspector]
    public Vector3 baseLocalPos;
    // ===============================
    // Init
    // ===============================
    void Awake()
    {
        Bind();
        mat = countryMesh.material;
        mat.color = baseColor;

        baseLocalPos = transform.localPosition;
    }
    [ContextMenu("Bind CountryNode")]
    void Bind()
    {
        countryId = gameObject.name;

        center = transform.Find("Center");
        if (center == null)
            Debug.LogError($"[CountryNode] Center missing on {name}");

        Transform countryTf = transform.Find("Country");
        if (countryTf == null)
        {
            Debug.LogError($"[CountryNode] Country object missing on {name}");
            return;
        }

        countryMesh = countryTf.GetComponent<MeshRenderer>();
        if (countryMesh == null)
            Debug.LogError($"[CountryNode] MeshRenderer missing on Country in {name}");
    }

    // ===============================
    // Camera Helper
    // ===============================
    public float GetSuggestedZoom(float padding = 1.2f)
    {
        if (countryMesh == null)
            return 22f;

        Bounds b = countryMesh.bounds;

        float halfHeight = b.size.y * 0.5f;
        float halfWidth = b.size.x * 0.5f;

        float aspect = (float)Screen.width / Screen.height;
        float sizeByWidth = halfWidth / aspect;

        float size = Mathf.Max(halfHeight, sizeByWidth);
        return size * padding;
    }

    // ===============================
    // 🔥 핵심: 5단계 정복 색 연출
    // ===============================
    public void PlayConquerStepAnimation(int beforeCleared, int afterCleared)
    {
        Debug.Log(
      $"[ConquerAnim] START country={countryId} " +
      $"before={beforeCleared}, after={afterCleared}, steps={conquerStepCount}"
  );

        if (stepRoutine != null)
            StopCoroutine(stepRoutine);

        stepRoutine = StartCoroutine(
            ConquerStepRoutine(beforeCleared, afterCleared)
        );
    }

    IEnumerator ConquerStepRoutine(int beforeCleared, int afterCleared)
    {
        float startT = Mathf.Clamp01((float)beforeCleared / stagesToConquer);
        float targetT = Mathf.Clamp01((float)afterCleared / stagesToConquer);

        for (int i = 1; i <= conquerStepCount; i++)
        {
            float stepT = Mathf.Lerp(
                startT,
                targetT,
                (float)i / conquerStepCount
            );

            Color stepColor = Color.Lerp(
                baseColor,
                conqueredColor,
                stepT
            );

            yield return StartCoroutine(
                LerpColor(stepColor, stepLerpDuration)
            );
        }
    }

    IEnumerator LerpColor(Color target, float duration)
    {
        Color start = mat.color;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            mat.color = Color.Lerp(start, target, t);
            yield return null;
        }

        mat.color = target;
    }

    // ===============================
    // 즉시 상태 반영 (로비 진입용)
    // ===============================
    public void ApplyInstantProgress(int cleared)
    {
        mat.color = GetStageColor(cleared);
    }
    public void PrepareConquerStepAnimation(
    int beforeCleared,
    int afterCleared,
    int stepCount
)
    {
        totalSteps = Mathf.Max(1, stepCount);
        currentStep = 0;

        float startT = (float)beforeCleared / stagesToConquer;
        float targetT = (float)afterCleared / stagesToConquer;

        startColor = GetStageColor(beforeCleared);
        targetColor = GetStageColor(afterCleared);

        mat.color = startColor;

      

        Debug.Log(
            $"[CountryNode] PrepareConquerAnim steps={totalSteps} " +
            $"startT={startT:F2} targetT={targetT:F2}"
        );
    }
    public void OnZombieExplode()
    {
        if (currentStep >= totalSteps)
            return;

        currentStep++;

        float t = (float)currentStep / totalSteps;
        Color newColor = Color.Lerp(startColor, targetColor, t);
        mat.color = newColor;

        Debug.Log(
            $"[CountryNode] Step {currentStep}/{totalSteps}"
        );

        // 🔥 마지막 스텝이면 연출 종료 알림
        if (currentStep >= totalSteps)
        {
            Debug.Log("[CountryNode] Conquer animation FINISHED");
            OnConquerAnimationFinished?.Invoke();
        }
    }
    public void OnConquerZombieExploded()
    {
        currentStep++;

        float t = (float)currentStep / totalSteps;

        Color newColor = Color.Lerp(startColor, targetColor, t);
        mat.color = newColor;

        if (currentStep >= totalSteps)
        {
            OnConquerAnimationFinished?.Invoke();
        }
    }

    Color GetStageColor(int stage)
    {
        float t = Mathf.Clamp01((float)stage / stagesToConquer);
        return Color.Lerp(baseColor, conqueredColor, t);
    }
}
