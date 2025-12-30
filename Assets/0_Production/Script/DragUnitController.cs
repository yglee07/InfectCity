using UnityEngine;
using UnityEngine.AI;

public class DragUnitController : MonoBehaviour
{
    public string unitPoolKey = "GreenZombie";
    public LayerMask groundMask;

    private GameObject previewUnit;
    private bool uiDragging;

    public void BeginUIDrag()
    {
        if (previewUnit == null)
        {
            previewUnit = PoolManager.Instance.Spawn(
                unitPoolKey,
                Vector3.zero,
                Quaternion.identity
            );

            SetPreviewMode(previewUnit, true);
        }

        uiDragging = true;
        previewUnit.SetActive(true);
    }

    public void UpdatePreviewByScreenPos(Vector2 screenPos)
    {
        if (!uiDragging || previewUnit == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
        {
            previewUnit.transform.position = hit.point;
        }
    }

    public void EndUIDrag(Vector2 screenPos)
    {
        uiDragging = false;

        if (previewUnit == null) return;

        // 프리뷰 → 실제 유닛으로 전환
        SetPreviewMode(previewUnit, false);
        previewUnit = null;
    }

    public void CancelUIDrag()
    {
        uiDragging = false;

        if (previewUnit != null)
        {
            PoolManager.Instance.Despawn(unitPoolKey, previewUnit);
            previewUnit = null;
        }
    }

    void SetPreviewMode(GameObject unit, bool preview)
{
    // 1️⃣ NavMeshAgent OFF
    if (unit.TryGetComponent<NavMeshAgent>(out var agent))
    {
        agent.enabled = !preview;
    }

    // 2️⃣ AI / 로직 스크립트 OFF
    if (unit.TryGetComponent<ZombieNavMesh>(out var zombie))
    {
        zombie.enabled = !preview;
    }



    // 4️⃣ Rigidbody 있으면 Kinematic
    
    // 5️⃣ 시각적 처리
    //SetGhostVisual(unit, preview);
}


    void SetGhostVisual(GameObject unit, bool ghost)
    {
        foreach (var r in unit.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in r.materials)
            {
                Color c = mat.color;
                c.a = ghost ? 0.4f : 1f;
                mat.color = c;
            }
        }
    }
}
