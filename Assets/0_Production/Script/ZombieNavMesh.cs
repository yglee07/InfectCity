using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMesh : MonoBehaviour
{
    public ZombieStats stats;

    [Header("Infect Settings")]
    public float infectDistance = 1.2f;

    [Header("Move Speeds")]
    [SerializeField] private float walkSpeed = 1.8f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float chaseDistance = 5f;

    [Header("Pool")]
    public string zombiePoolKey = "Zombie";

    private NavMeshAgent agent;
    [SerializeField] private CitizenNavMesh targetCitizen;

    private float retargetInterval = 0.2f;
    private float timer;
    private Vector3 lastTargetPos;
    private Animator anim;

    // 현재 재생 중인 애니메이션 Trigger 이름
    private string currentAnim = "";

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = 20f;
        agent.autoBraking = false;

        anim = GetComponentInChildren<Animator>();
    }
  

    public void ApplyStats()
    {
        if (stats == null) return;
        if (agent == null) return;


        infectDistance = stats.infectDistance * stats.sizeMultiplier; // 자동 반영 추천

        walkSpeed = stats.walkSpeed;
        runSpeed = stats.runSpeed;
        chaseDistance = stats.chaseDistance;

        // 크기 적용
        transform.localScale = Vector3.one * stats.sizeMultiplier;

        // 애니메이션 속도
        if (anim != null)
            anim.speed = stats.animSpeed;

        // NavMeshAgent 초기 속도 설정
        agent.speed = walkSpeed;
    }
    void OnEnable()
    {
        if (NPCManager.Instance != null)
            NPCManager.Instance.RegisterZombie(this);

        ApplyStats();
    }

    void OnDisable()
    {
        if (NPCManager.Instance != null)
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
    }

    // ------- ANIMATION HELPER -------
    void PlayAnim(string trigger)
    {
        if (currentAnim == trigger) return; // 중복 재생 방지

        currentAnim = trigger;

        anim.ResetTrigger("Idle");
        anim.ResetTrigger("Walk");
        anim.ResetTrigger("Run");

        anim.SetTrigger(trigger);
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

        // 타겟 없으면 Idle 상태
        if (nearest == null)
        {
            agent.isStopped = true;
            targetCitizen = null;
            agent.ResetPath();
            PlayAnim("Idle");   // ← 여기서 Idle 재생
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

        // 속도 / 애니메이션 결정
        HandleSpeedBasedOnDistance(nearest);
    }

    // ---------------- SPEED / ANIMATION LOGIC ----------------
    void HandleSpeedBasedOnDistance(CitizenNavMesh target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);

        // 가까우면 Run
        if (dist <= chaseDistance)
        {
            agent.speed = runSpeed;
            PlayAnim("Run");
        }
        else
        {
            agent.speed = walkSpeed;
            PlayAnim("Walk");
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
        string key = NPCManager.Instance.GetZombiePoolKey();

        var zombie = PoolManager.Instance.Spawn(key, spawnPos, Quaternion.identity);

        // 추적 초기화
        agent.isStopped = true;
        targetCitizen = null;
        agent.ResetPath();

        // 새 타겟 즉시 탐색
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
