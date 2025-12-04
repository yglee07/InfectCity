using UnityEngine;

public class DragInfectController : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 2f;
    public LayerMask groundMask;

    [Header("Preview Settings")]
    public Color previewColor = new Color(1f, 0f, 0f, 0.3f);
    public int circleSegments = 32;

    private Vector2 touchStartPos;
    private bool isDragging = false;
    private LineRenderer circleRenderer;
    private Vector3 previewWorldPos;

    void Start()
    {
        CreateCircleRenderer();
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif

        // 드래그 중이면 원형 위치 업데이트
        if (isDragging)
            UpdateCirclePreview();
    }

    // ============================
    //      원형 표시 라인렌더러
    // ============================
    void CreateCircleRenderer()
    {
        GameObject obj = new GameObject("ExplosionPreviewCircle");
        circleRenderer = obj.AddComponent<LineRenderer>();

        circleRenderer.positionCount = circleSegments + 1;
        circleRenderer.loop = true;
        circleRenderer.startWidth = 0.05f;
        circleRenderer.endWidth = 0.05f;
        circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
        circleRenderer.startColor = previewColor;
        circleRenderer.endColor = previewColor;

        circleRenderer.enabled = false; // 기본은 숨김
    }

    void UpdateCirclePreview()
    {
        Vector3 worldPos;
        if (!TryGetWorldPosition(Input.mousePosition, out worldPos))
            return;

        previewWorldPos = worldPos;

        // 원형 보이게
        if (!circleRenderer.enabled)
            circleRenderer.enabled = true;

        DrawCircle(previewWorldPos, explosionRadius);
    }

    void DrawCircle(Vector3 center, float radius)
    {
        float angle = 0f;
        for (int i = 0; i <= circleSegments; i++)
        {
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            circleRenderer.SetPosition(i, center + new Vector3(x, 0.05f, z));
            angle += (2 * Mathf.PI) / circleSegments;
        }
    }

    void HideCircle()
    {
        if (circleRenderer != null)
            circleRenderer.enabled = false;
    }


    // ============================
    //     PC Editor 입력 처리
    // ============================
    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag(Input.mousePosition);
            isDragging = false;
        }
    }

    // ============================
    //     모바일 터치 입력
    // ============================
    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch t = Input.GetTouch(0);

        switch (t.phase)
        {
            case TouchPhase.Began:
                touchStartPos = t.position;
                isDragging = true;
                break;

            case TouchPhase.Ended:
                if (isDragging)
                    EndDrag(t.position);
                isDragging = false;
                break;
        }
    }

    // ============================
    //     드래그 종료
    // ============================
    void EndDrag(Vector2 screenPos)
    {
        HideCircle();

        Vector3 worldPos;
        if (!TryGetWorldPosition(screenPos, out worldPos))
            return;

        InfectArea(worldPos, explosionRadius);

        Debug.DrawRay(worldPos, Vector3.up * 2f, Color.red, 1f);
    }

    // ============================
    //   화면 → 월드 변환
    // ============================
    bool TryGetWorldPosition(Vector2 screenPos, out Vector3 worldPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            worldPos = hit.point;
            return true;
        }

        worldPos = Vector3.zero;
        return false;
    }

    // ============================
    //     범위 내 시민 감염
    // ============================
    void InfectArea(Vector3 center, float radius)
    {
        var citizens = NPCManager.Instance.Citizens;

        float r2 = radius * radius;

        for (int i = citizens.Count - 1; i >= 0; i--)
        {
            var c = citizens[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            float dist = (c.transform.position - center).sqrMagnitude;

            if (dist <= r2)
            {
                Vector3 spawnPos = c.transform.position;

                c.Infect();
                PoolManager.Instance.Spawn("Zombie", spawnPos, Quaternion.identity);
            }
        }
    }
}
