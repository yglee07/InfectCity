using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMesh : MonoBehaviour
{
    [Header("Infect Settings")]
    public float infectDistance = 1.2f;

    [Header("Pool")]
    public string zombiePoolKey = "Zombie";

    private NavMeshAgent agent;

    [SerializeField]
    private CitizenNavMesh targetCitizen;

    private float retargetInterval = 0.2f;
    private float timer;
    private Vector3 lastTargetPos;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = 20f;
        agent.autoBraking = false;
    }
    void OnEnable()
    {
        NPCManager.Instance.RegisterZombie(this);
    }

    void OnDisable()
    {
        NPCManager.Instance.UnregisterZombie(this);
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 주기적으로 타겟 재탐색
        if (timer >= retargetInterval)
        {
            timer = 0f;
            FindNearestCitizen();
        }

        TryInfect();
    }

    // ------------------- FIND CITIZEN (GLOBAL LIST) -------------------
    void FindNearestCitizen()
    {
        CitizenNavMesh nearest = null;
        float minSqr = float.MaxValue;

        var list = NPCManager.Instance.Citizens;
        int count = list.Count;
    
        for (int i = 0; i < count; i++)
        {
            var c = list[i];
            if (c == null) continue;
            if (!c.gameObject.activeInHierarchy) continue;
    
            Vector3 delta = c.transform.position - transform.position;
            float sqr = delta.sqrMagnitude;
    
            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = c;
            }
        }

        if (nearest == null)
        {
            agent.isStopped = true;      // ★ 즉시 멈춤
            targetCitizen = null;
            agent.ResetPath();
            return;
        }

        // 타겟 변경 또는 크게 움직였을 때만 목적지 갱신
        if (targetCitizen != nearest)
        {
            targetCitizen = nearest;
            lastTargetPos = nearest.transform.position;
            agent.SetDestination(lastTargetPos);
        }
        else
        {
            Vector3 nowPos = nearest.transform.position;
            if ((nowPos - lastTargetPos).sqrMagnitude > 0.25f) // 0.5m 이상 이동 시
            {
                lastTargetPos = nowPos;
                agent.SetDestination(lastTargetPos);
            }
        }
    }
 
    // ------------------- INFECT -------------------
    void TryInfect()
    {
        if (targetCitizen == null) return;
        if (!targetCitizen.gameObject.activeInHierarchy) return;

        float sqrDist = (targetCitizen.transform.position - transform.position).sqrMagnitude;
        float infectSqr = infectDistance * infectDistance;

        if (sqrDist > infectSqr) return;

        Vector3 spawnPos = targetCitizen.transform.position;

        // Citizen 감염 처리
        targetCitizen.Infect();  // 내부에서 SetActive(false) + 리스트 제거

        // 풀에서 좀비 생성
        GameObject zombie = PoolManager.Instance.Spawn(zombiePoolKey, spawnPos, Quaternion.identity);

        // 다음 타겟 초기화 및 즉시 재탐색
        agent.isStopped = true;      // ★ 즉시 멈춤
        targetCitizen = null;
        agent.ResetPath();
        FindNearestCitizen();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (agent == null) return;
        if (!agent.hasPath) return;

        Gizmos.color = Color.cyan;
        Vector3[] corners = agent.path.corners;

        for (int i = 0; i < corners.Length; i++)
            Gizmos.DrawSphere(corners[i], 0.15f);

        for (int i = 0; i < corners.Length - 1; i++)
            Gizmos.DrawLine(corners[i], corners[i + 1]);
    }
}
