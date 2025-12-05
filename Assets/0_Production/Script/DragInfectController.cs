using UnityEngine;

public class DragInfectController : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 2f;
    public LayerMask groundMask;

    [Header("Quad Preview Settings")]
    public Material previewMaterial; // Unlit Transparent 원 텍스처
    public float quadHeightOffset = 0.02f; // 지면 떠있는 정도

    private Vector2 touchStartPos;
    private bool isDragging = false;

    private Transform previewQuad;   // 미리보기 Quad
    private MeshRenderer quadRenderer;
    private Vector3 previewWorldPos;

    [Header("Preview Colors")]
    public Color normalColor = new Color(0f, 1f, 0f, 0.3f);
    public Color alertColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("Charges")]
    public int maxCharges = 1;
    public int currentCharges;
    void Start()
    {
        CreatePreviewQuad();
    }

    void Update()
    {
        if (currentCharges <= 0)
        {
            HideQuad();
            return;
        }

#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif

        if (isDragging)
            UpdateQuadPreview();
    }

    // =========================================
    //   Quad 생성
    // =========================================
    void CreatePreviewQuad()
    {
        GameObject quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObj.name = "ExplosionPreviewQuad";

        // 필요한 컴포넌트만 유지
        Destroy(quadObj.GetComponent<Collider>());

        quadRenderer = quadObj.GetComponent<MeshRenderer>();
        quadRenderer.material = previewMaterial;

        previewQuad = quadObj.transform;
        previewQuad.gameObject.SetActive(false);

        // 기본 스케일(1,1,1) → 나중에 radius에 맞게 조절
    }

    // =========================================
    //   Quad Preview 업데이트
    // =========================================
    void UpdateQuadPreview()
    {
        Vector3 worldPos;
        if (!TryGetWorldPosition(Input.mousePosition, out worldPos))
            return;

        previewWorldPos = worldPos;

        if (!previewQuad.gameObject.activeSelf)
            previewQuad.gameObject.SetActive(true);

        // 위치 적용
        previewQuad.position = worldPos + Vector3.up * quadHeightOffset;

        // 지형 normal 따라 기울기 맞추기
        if (Physics.Raycast(worldPos + Vector3.up, Vector3.down, out RaycastHit hit, 5f, groundMask))
        {
            previewQuad.rotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
        }

        // 스케일 적용 (Quad는 1x1 단위)
        float scaled = explosionRadius * 2f;
        previewQuad.localScale = new Vector3(scaled, scaled, 1f);

        bool detected = IsCitizenInside(previewWorldPos, explosionRadius);
        quadRenderer.material.color = detected ? alertColor : normalColor;
    }

    void HideQuad()
    {
        if (previewQuad != null)
            previewQuad.gameObject.SetActive(false);
    }

    // =========================================
    //   Mouse input (Editor)
    // =========================================
    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag(Input.mousePosition);
            isDragging = false;
        }
    }

    // =========================================
    //   Touch input (Mobile)
    // =========================================
    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch t = Input.GetTouch(0);

        switch (t.phase)
        {
            case TouchPhase.Began:
                isDragging = true;
                break;

            case TouchPhase.Ended:
                if (isDragging)
                    EndDrag(t.position);
                isDragging = false;
                break;
        }
    }

    // =========================================
    //   Drag End
    // =========================================
    void EndDrag(Vector2 screenPos)
    {
        if (currentCharges <= 0)
        {
            HideQuad();
            return;
        }


        HideQuad();

        Vector3 worldPos;
        if (!TryGetWorldPosition(screenPos, out worldPos))
            return;

        InfectArea(worldPos, explosionRadius);
        currentCharges--;
        Game.Instance.uiGame.UpdateCharges(currentCharges, maxCharges);
    }

    // =========================================
    //   Screen → World
    // =========================================
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

    // =========================================
    //   Citizens Infect
    // =========================================
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
                string key = NPCManager.Instance.GetZombiePoolKey();

                var zombie = PoolManager.Instance.Spawn(key, spawnPos, Quaternion.identity);

            }
        }
    }

    bool IsCitizenInside(Vector3 center, float radius)
    {
        float r2 = radius * radius;
        var citizens = NPCManager.Instance.Citizens;

        for (int i = citizens.Count - 1; i >= 0; i--)
        {
            var c = citizens[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            float dist = (c.transform.position - center).sqrMagnitude;
            if (dist <= r2) return true;
        }

        return false;
    }

}
