using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMesh : MonoBehaviour
{
    [Header("Infect Settings")]
    public float infectDistance = 1.2f;

    [Header("Move Speeds (BlendTree 기준)")]
    [SerializeField] private float walkSpeed = 1.8f;     // 더 빠른 Walk
    [SerializeField] private float runSpeed = 4f;        // Run 유지
    [SerializeField] private float chaseDistance = 5f;   // 5m 이내면 Run

    [Header("Pool")]
    public string zombiePoolKey = "Zombie";

    private NavMeshAgent agent;
    [SerializeField] private CitizenNavMesh targetCitizen;

    private float retargetInterval = 0.2f;
    private float timer;
    private Vector3 lastTargetPos;
    private Animator anim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = 20f;
        agent.autoBraking = false;

        anim = GetComponentInChildren<Animator>();
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

        // 타겟 재탐색
        if (timer >= retargetInterval)
        {
            timer = 0f;
            FindNearestCitizen();
        }

        TryInfect();

        // BlendTree MoveSpeed 업데이트
        anim.SetFloat("MoveSpeed", agent.velocity.magnitude);
    }

    // ---------------- FIND NEAREST CITIZEN ----------------
    void FindNearestCitizen()
    {
        CitizenNavMesh nearest = null;
        float minSqr = float.MaxValue;

        var list = NPCManager.Instance.Citizens;
        int count = list.Count;

        for (int i = 0; i < count; i++)
        {
            var c = list[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            float sqr = (c.transform.position - transform.position).sqrMagnitude;

            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = c;
            }
        }

        // 타겟 없으면 멈춤
        if (nearest == null)
        {
            agent.isStopped = true;
            targetCitizen = null;
            agent.ResetPath();
            return;
        }

        // 타겟 변경 시 목적지 업데이트
        if (targetCitizen != nearest)
        {
            targetCitizen = nearest;
            lastTargetPos = nearest.transform.position;
            agent.SetDestination(lastTargetPos);
            agent.isStopped = false;
        }
        else
        {
            // 타겟이 이동하면 목적지 갱신
            Vector3 nowPos = nearest.transform.position;
            if ((nowPos - lastTargetPos).sqrMagnitude > 0.25f)
            {
                lastTargetPos = nowPos;
                agent.SetDestination(lastTargetPos);
                agent.isStopped = false;
            }
        }

        // ★ 이동 속도 / 애니메이션 결정
        HandleSpeedBasedOnDistance(nearest);
    }

    // ---------------- SPEED / ANIMATION LOGIC ----------------
    void HandleSpeedBasedOnDistance(CitizenNavMesh target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);

        // 5m 이내 → Run
        if (dist <= chaseDistance)
        {
            agent.speed = runSpeed;
        }
        // 멀리 있으면 → 빠르게 Walk
        else
        {
            agent.speed = walkSpeed;
        }
    }

    // ---------------- INFECT ----------------
    void TryInfect()
    {
        if (targetCitizen == null) return;
        if (!targetCitizen.gameObject.activeInHierarchy) return;

        float sqrDist = (targetCitizen.transform.position - transform.position).sqrMagnitude;
        float infectSqr = infectDistance * infectDistance;

        if (sqrDist > infectSqr) return;

        Vector3 spawnPos = targetCitizen.transform.position;

        // 시민 감염
        targetCitizen.Infect();

        // 새 좀비 생성
        PoolManager.Instance.Spawn(zombiePoolKey, spawnPos, Quaternion.identity);

        // 추적 초기화
        agent.isStopped = true;
        targetCitizen = null;
        agent.ResetPath();
        FindNearestCitizen();
    }

    // ---------------- DEBUG ----------------
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
