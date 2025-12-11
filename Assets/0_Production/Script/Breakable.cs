using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class Breakable : MonoBehaviour
{
    public int maxHP = 10;
    public int currentHP;

    // Collider 없이 거리 계산용 AABB 박스 크기
    public Vector3 size;

    private NavMeshObstacle obstacle;

    void Awake()
    {
        currentHP = maxHP;

        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;

        // ───────────────────────────────
        // 🔥 Breakable 크기 자동 계산 (Collider 없어도 됨)
        // ───────────────────────────────
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            size = rend.bounds.size;
        else
            size = new Vector3(1, 1, 1); // fallback
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;

        if (currentHP <= 0)
            Break();
    }

    private void Break()
    {
        // NavMesh 경로 즉시 복구
        if (obstacle != null)
            obstacle.enabled = false;

        gameObject.SetActive(false);
    }
}
