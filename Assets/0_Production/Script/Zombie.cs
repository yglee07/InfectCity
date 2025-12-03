using UnityEngine;

public class Zombie : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 2.5f;
    public float rotateSpeed = 6f;

    [Header("시민 탐지 설정")]
    public float detectRadius = 8f;
    public LayerMask citizenLayer;

    [Header("좀비 Separation 설정")]
    public float separationRadius = 2f;
    public float separationWeight = 2.5f;

    private Transform currentTarget;

    void Update()
    {
        FindNearestCitizen();

        if (currentTarget != null)
            MoveTowards(currentTarget.position);
    }

    // =============== 시민 탐지 ===============
    void FindNearestCitizen()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, citizenLayer);

        if (hits.Length == 0)
        {
            currentTarget = null;
            return;
        }

        Transform nearest = null;
        float shortest = Mathf.Infinity;

        foreach (var h in hits)
        {
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < shortest)
            {
                shortest = d;
                nearest = h.transform;
            }
        }

        currentTarget = nearest;
    }

    // =============== 이동 ===============
    void MoveTowards(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;

        Vector3 desiredDir = dir.normalized;

        // Separation force 추가 (좀비끼리 안 겹침)
        desiredDir += GetSeparationDir() * separationWeight;

        Vector3 finalDir = desiredDir.normalized;

        // 회전
        if (finalDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(finalDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // =============== Separation 구현 ===============
    Vector3 GetSeparationDir()
    {
        Vector3 separate = Vector3.zero;
        int count = 0;

        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (!hit.CompareTag("Zombie")) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < 0.001f) continue;

            // 가까워질수록 강한 밀어내기
            Vector3 away = (transform.position - hit.transform.position).normalized;
            away /= dist; // inverse distance weighting

            separate += away;
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        return separate.normalized;
    }

    // =============== Debug Gizmos ===============
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Separation radius 표시
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // 탐지 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
