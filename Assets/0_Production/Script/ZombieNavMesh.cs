using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMesh : MonoBehaviour
{
    [Header("Settings")]
    public float detectRadius = 10f;
    public float infectDistance = 1.2f;
    public LayerMask citizenLayer;

    [Header("Prefabs")]
    public GameObject zombiePrefab; // 감염 시 생성할 좀비

    private NavMeshAgent agent;
    private Transform targetCitizen;
    private float retargetInterval = 0.1f;
    private float timer;
    private Vector3 lastTargetPos;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = 20f;
        agent.autoBraking = false;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= retargetInterval)
        {
            timer = 0f;
            FindNearestCitizen();
        }

        TryInfect();
    }

    // ------------------- FIND CITIZEN -------------------
    void FindNearestCitizen()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, citizenLayer);

        if (hits.Length == 0)
        {
            targetCitizen = null;
            return;
        }

        Transform nearest = hits[0].transform;
        float min = Vector3.Distance(transform.position, nearest.position);

        foreach (var h in hits)
        {
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < min)
            {
                min = d;
                nearest = h.transform;
            }
        }

        if (targetCitizen != nearest)
        {
            targetCitizen = nearest;
            lastTargetPos = nearest.position;
            agent.SetDestination(lastTargetPos);
        }
        else
        {
            if (Vector3.Distance(lastTargetPos, nearest.position) > 0.5f)
            {
                lastTargetPos = nearest.position;
                agent.SetDestination(lastTargetPos);
            }
        }
    }

    // ------------------- INFECT -------------------
    void TryInfect()
    {
        if (!targetCitizen) return;

        if (Vector3.Distance(transform.position, targetCitizen.position) <= infectDistance)
        {
            Vector3 pos = targetCitizen.position;

            targetCitizen.gameObject.SetActive(false);
            Instantiate(zombiePrefab, pos, Quaternion.identity);

            targetCitizen = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (agent == null) return;
        if (!agent.hasPath) return;

        Gizmos.color = Color.cyan;

        Vector3[] corners = agent.path.corners;

        // 코너 지점들 구 시각화
        for (int i = 0; i < corners.Length; i++)
            Gizmos.DrawSphere(corners[i], 0.15f);

        // 선 연결
        for (int i = 0; i < corners.Length - 1; i++)
            Gizmos.DrawLine(corners[i], corners[i + 1]);
    }
}
