using System.Collections.Generic;
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

    [Header("Effects")]
    public GameObject explosionEffectPrefab;

    private List<CitizenNavMesh> highlighted = new List<CitizenNavMesh>();
    private float baseRadius;
    public bool IsDraggingBomb => uiDragging;
    public float FinalRadius => baseRadius * UpgradeManager.Instance.GetBombRadiusMultiplier();
    void Start()
    {
        baseRadius = explosionRadius;   // 인스펙터 기본값 저장
   
        CreatePreviewQuad();
    }

    void Update()
    {
        // 폭탄 UI에서 끌어올 때만 동작하게
        if (!uiDragging)
        {
            HideQuad();
            ClearHighlights();
            return;
        }

        if (currentCharges <= 0)
        {
            HideQuad();
            return;
        }

        // 여기서는 더 이상 HandleMouse/HandleTouch 필요 없음
        // 드래그 위치는 BombButton에서 UpdatePreviewByScreenPos로 들어옴
    }
    private bool isActive = false;

    public void Activate()
    {
        isActive = true;
    }

    public void Deactivate()
    {
        isActive = false;
        HideQuad();
        ClearHighlights();
    }
    // UI에서 활성화 여부
    private bool uiDragging = false;

    // UI에서 폭탄 드래그 시작
    public void BeginUIDrag()
    {
        if (currentCharges <= 0) return;

        uiDragging = true;
        isDragging = true;          // 기존 플래그 그대로 활용
    }

    // UI 드래그 중 위치 업데이트
    public void UpdatePreviewByScreenPos(Vector2 screenPos)
    {
        if (!uiDragging) return;

        Vector3 worldPos;
        if (!TryGetWorldPosition(screenPos, out worldPos))
            return;

        // 기존 UpdateQuadPreview 내용을 worldPos 기반으로 옮겨온 느낌
        previewWorldPos = worldPos;

        if (!previewQuad.gameObject.activeSelf)
            previewQuad.gameObject.SetActive(true);

        float offsetZ = 3f;
        Vector3 offsetPos = worldPos + new Vector3(0, 0, offsetZ);

        previewQuad.position = offsetPos + Vector3.up * quadHeightOffset;

        if (Physics.Raycast(worldPos + Vector3.up, Vector3.down, out RaycastHit hit, 5f, groundMask))
        {
            previewQuad.rotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
        }

        float scaled = FinalRadius * 2f;
        previewQuad.localScale = new Vector3(scaled, scaled, 1f);

        UpdateCitizenHighlights(offsetPos,FinalRadius);

        bool detected = IsCitizenInside(offsetPos, FinalRadius);
        quadRenderer.material.color = detected ? alertColor : normalColor;
    }

    // UI 드래그 끝(손 뗐을 때)
    public void EndUIDrag(Vector2 screenPos)
    {
        if (!uiDragging) return;

        uiDragging = false;
        isDragging = false;

        EndDrag(screenPos);   // 기존 로직 재사용
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
    //void UpdateQuadPreview()
    //{
    //    Vector3 worldPos;
    //    if (!TryGetWorldPosition(Input.mousePosition, out worldPos))
    //        return;

    //    previewWorldPos = worldPos;

    //    if (!previewQuad.gameObject.activeSelf)
    //        previewQuad.gameObject.SetActive(true);

    //    // 위치 적용
    //    float offsetZ = 3f;
    //    Vector3 offsetPos = worldPos + new Vector3(0, 0, offsetZ);

    //    previewQuad.position = offsetPos + Vector3.up * quadHeightOffset;

    //    // 지형 normal 따라 기울기 맞추기
    //    if (Physics.Raycast(worldPos + Vector3.up, Vector3.down, out RaycastHit hit, 5f, groundMask))
    //    {
    //        previewQuad.rotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
    //    }

    //    // 스케일 적용 (Quad는 1x1 단위)
    //    float scaled = explosionRadius * 2f;
    //    previewQuad.localScale = new Vector3(scaled, scaled, 1f);

    //    UpdateCitizenHighlights(worldPos);

    //    bool detected = IsCitizenInside(previewWorldPos, explosionRadius);
    //    quadRenderer.material.color = detected ? alertColor : normalColor;
    //}

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
        ClearHighlights();

        if (currentCharges <= 0)
        {
            HideQuad();
            Deactivate();   // ← 폭탄 사용 불가 상태로 자동 종료
            return;
        }


        HideQuad();

        Vector3 worldPos;
        if (!TryGetWorldPosition(screenPos, out worldPos))
            return;

        float offsetZ = 3f;  // 너가 원하는 값으로 조절
        Vector3 infectPos = worldPos + new Vector3(0, 0, offsetZ);

        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab);
            fx.transform.position = infectPos + new Vector3(0, 1f, 0);

            float scale = FinalRadius / baseRadius;
            fx.transform.localScale *= scale;

          
        }

        InfectArea(infectPos, FinalRadius);
        currentCharges--;
        Game.Instance.uiGame.UpdateCharges(currentCharges, maxCharges);

        Deactivate();
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
    void UpdateCitizenHighlights(Vector3 center,float radius)
    {
        float r2 = radius * radius;
        var citizens = NPCManager.Instance.Citizens;

        List<CitizenNavMesh> inside = new List<CitizenNavMesh>();

        for (int i = 0; i < citizens.Count; i++)
        {
            var c = citizens[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            float dist = (c.transform.position - center).sqrMagnitude;

            if (dist <= r2)
            {
                inside.Add(c);

                if (!highlighted.Contains(c))
                {
                    highlighted.Add(c);
                    c.GetComponent<OutlineController>()?.SetHighlight(true);
                }
            }
        }

        // 범위 벗어난 시민 하이라이트 제거
        for (int i = highlighted.Count - 1; i >= 0; i--)
        {
            var c = highlighted[i];
            if (!inside.Contains(c))
            {
                c.GetComponent<OutlineController>()?.SetHighlight(false);
                highlighted.RemoveAt(i);
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var c in highlighted)
            c.GetComponent<OutlineController>()?.SetHighlight(false);

        highlighted.Clear();
    }
}
