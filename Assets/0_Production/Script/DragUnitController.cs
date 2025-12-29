using UnityEngine;

public class DragUnitController : MonoBehaviour
{
    [Header("Unit")]
    public GameObject unitPrefab;          // 실제 생성될 유닛
    public GameObject previewPrefab;       // 반투명 프리뷰 유닛

    [Header("Placement")]
    public LayerMask groundMask;
    public float heightOffset = 0.02f;

    [Header("Charges")]
    public int maxCharges = 1;
    public int currentCharges = 1;

    private Transform preview;
    private bool uiDragging;

    void Start()
    {
        CreatePreview();
    }

    // ================================
    // UI Drag Control
    // ================================
    public void BeginUIDrag()
    {
        if (currentCharges <= 0)
            return;

        uiDragging = true;
    }

    public void UpdatePreviewByScreenPos(Vector2 screenPos)
    {
        if (!uiDragging)
            return;

        if (!TryGetWorldPosition(screenPos, out Vector3 worldPos))
            return;

        if (!preview.gameObject.activeSelf)
            preview.gameObject.SetActive(true);

        preview.position = worldPos + Vector3.up * heightOffset;
    }

    public void EndUIDrag(Vector2 screenPos)
    {
        if (!uiDragging)
            return;

        uiDragging = false;

        if (preview != null)
            preview.gameObject.SetActive(false);

        if (currentCharges <= 0)
            return;

        Vector3 spawnPos = preview.position;

        Instantiate(unitPrefab, spawnPos, Quaternion.identity);

        currentCharges--;
    }

    public void CancelUIDrag()
    {
        uiDragging = false;

        if (preview != null)
            preview.gameObject.SetActive(false);
    }

    // ================================
    // Preview
    // ================================
    void CreatePreview()
    {
        if (previewPrefab == null)
        {
            Debug.LogError("Preview Prefab is NULL");
            return;
        }

        GameObject obj = Instantiate(previewPrefab);
        preview = obj.transform;
        preview.gameObject.SetActive(false);
    }

    // ================================
    // Screen → World
    // ================================
    bool TryGetWorldPosition(Vector2 screenPos, out Vector3 worldPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            worldPos = hit.point;
            return true;
        }

        worldPos = ray.GetPoint(10f);
        return true;
    }
}
