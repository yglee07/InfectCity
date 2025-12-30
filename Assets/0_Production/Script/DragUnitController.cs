using UnityEngine;
using UnityEngine.AI;

public class DragUnitController : MonoBehaviour
{
    [Header("Spawn Settings")]
    public string unitPoolKey = "GreenZombie";
    public LayerMask groundMask;

    [Header("Charges")]
    public int maxCharges = 1;
    public int currentCharges;
    public bool IsDraggingUnit => uiDragging;
    // 드래그 상태
    private bool uiDragging = false;
    private GameObject previewUnit;

    void Start()
    {
        // 한 판 시작 시 리셋
        currentCharges = maxCharges;
    }

    // =========================
    // UI Drag 시작
    // =========================
    public void BeginUIDrag()
    {
        // 언락 조건
        if (!UnlockManager.IsUnlocked(UnlockType.DragUnit))
            return;

        // 사용 횟수 체크
        if (currentCharges <= 0)
            return;

        uiDragging = true;

        if (previewUnit == null)
        {
            previewUnit = PoolManager.Instance.Spawn(
                unitPoolKey,
                Vector3.zero,
                Quaternion.identity
            );

            SetPreviewMode(previewUnit, true);
        }

        previewUnit.SetActive(true);
    }

    // =========================
    // 드래그 중 위치 업데이트
    // =========================
    public void UpdatePreviewByScreenPos(Vector2 screenPos)
    {
        if (!uiDragging || previewUnit == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            previewUnit.transform.position = hit.point;
        }
    }

    // =========================
    // 드래그 종료 (소환 확정)
    // =========================
    public void EndUIDrag(Vector2 screenPos)
    {
        if (!uiDragging || previewUnit == null)
            return;

        uiDragging = false;

        // 프리뷰 → 실제 유닛 전환
        SetPreviewMode(previewUnit, false);

        previewUnit = null;
        currentCharges--;

       Game.Instance.uiGame.RefreshActionButtons(
    Game.Instance.dragInfector.currentCharges,
    currentCharges
);
    }

    // =========================
    // 드래그 취소
    // =========================
    public void CancelUIDrag()
    {
        uiDragging = false;

        if (previewUnit != null)
        {
            PoolManager.Instance.Despawn(unitPoolKey, previewUnit);
            previewUnit = null;
        }
    }

    // =========================
    // Preview / Real 전환
    // =========================
    void SetPreviewMode(GameObject unit, bool preview)
    {
        // NavMeshAgent
        if (unit.TryGetComponent<NavMeshAgent>(out var agent))
            agent.enabled = !preview;

        // AI 로직
        if (unit.TryGetComponent<ZombieNavMesh>(out var zombie))
            zombie.enabled = !preview;

        // Rigidbody 있다면
        if (unit.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = preview;

        // 시각적 처리 (선택)
        SetGhostVisual(unit, preview);
    }

    void SetGhostVisual(GameObject unit, bool ghost)
    {
        foreach (var r in unit.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in r.materials)
            {
                if (!mat.HasProperty("_Color")) continue;

                Color c = mat.color;
                c.a = ghost ? 0.4f : 1f;
                mat.color = c;
            }
        }
    }

    // =========================
    // 외부 조회용
    // =========================
    public bool CanUse()
    {
        return UnlockManager.IsUnlocked(UnlockType.DragUnit)
               && currentCharges > 0;
    }
}
