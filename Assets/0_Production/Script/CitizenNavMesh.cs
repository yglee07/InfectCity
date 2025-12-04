using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CitizenNavMesh : MonoBehaviour
{
    public enum State { Wander, Flee }

    [Header("Idle Settings")]
    public float idleMin = 0.5f;
    public float idleMax = 2.0f;

    private bool isIdle = false;
    private float idleTimer = 0f;

    [Header("Speed")]
    public float wanderSpeed = 2f;
    public float fleeSpeed = 4f;

    [Header("Wander Settings")]
    public float wanderRadius = 6f;
    public float changeWanderInterval = 3f;

    [Header("Zombie Detection")]
    public float fleeEnterRadius = 4f;
    public float fleeExitRadius = 6f;
    public float fleeDistance = 7f;
    public LayerMask zombieLayer;

    private NavMeshAgent agent;
    private State state;
    private float timer;
    private Vector3 wanderTarget;
    private Vector3 fleeTarget;
    private Animator anim;

    void OnEnable()
    {
        if (NPCManager.Instance != null)
            NPCManager.Instance.RegisterCitizen(this);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    void OnDisable()
    {
        if (NPCManager.Instance != null)
            NPCManager.Instance.UnregisterCitizen(this);
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        ChangeState(State.Wander);
        SetNewWanderTarget();
    }

    void Update()
    {
        // 좀비 감지 → Idle 중이어도 무조건 우선순위
        bool detected = DetectZombie();
        if (detected && state != State.Flee)
            ChangeState(State.Flee);
        else if (!detected && state != State.Wander)
            ChangeState(State.Wander);

        // 상태별 업데이트
        if (state == State.Wander) UpdateWander();
        else UpdateFlee();

        // 애니메이션 처리
        if (isIdle)
        {
            anim.SetFloat("MoveSpeed", 0f);
        }
        else
        {
            anim.SetFloat("MoveSpeed", agent.velocity.sqrMagnitude);
        }
    }

    // ------------------- STATE CHANGE -------------------
    void ChangeState(State newState)
    {
        state = newState;
        if (newState == State.Flee)
            isIdle = false;

        agent.speed = (newState == State.Wander) ? wanderSpeed : fleeSpeed;

        if (newState == State.Wander)
        {
            isIdle = false; // Flee → Wander 복귀시 Idle 초기화
            SetNewWanderTarget();
        }
        else
        {
            SetNewFleeTarget();
        }
    }

    // ------------------- WANDER -------------------
    void UpdateWander()
    {
        // --- Idle 상태 ---
        if (isIdle)
        {
            // NavMeshAgent를 멈추지 말고, 그냥 자기 자리로 목적지를 고정
            agent.SetDestination(transform.position);

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                isIdle = false;
                SetNewWanderTarget();
            }
            return;
        }

        // --- Wander 로직 ---
        timer += Time.deltaTime;

        if (agent.remainingDistance <= 0.4f || timer >= changeWanderInterval)
        {
            timer = 0f;

            // Idle 랜덤 진입
            if (Random.value < 0.3f)
            {
                isIdle = true;
                idleTimer = Random.Range(idleMin, idleMax);
                return;
            }

            SetNewWanderTarget();
        }
    }

    void SetNewWanderTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        Vector3 target = transform.position + new Vector3(rnd.x, 0, rnd.y);

        if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
            agent.SetDestination(wanderTarget);
        }
    }

    // ------------------- FLEE -------------------
    void UpdateFlee()
    {
        // 도망 중이면 목적지에 도착할 때마다 갱신
        if (agent.remainingDistance <= 0.6f)
            SetNewFleeTarget();
    }

    void SetNewFleeTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, fleeEnterRadius, zombieLayer);
        if (hits.Length == 0)
        {
            ChangeState(State.Wander);
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

        Vector3 away = (transform.position - nearest.position).normalized;
        Vector3 raw = transform.position + away * fleeDistance;

        raw += new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

        if (NavMesh.SamplePosition(raw, out var hit, 2f, NavMesh.AllAreas))
        {
            fleeTarget = hit.position;
            agent.SetDestination(fleeTarget);
        }
    }

    // ------------------- ZOMBIE DETECTION -------------------
    bool DetectZombie()
    {
        float radius = (state == State.Flee) ? fleeExitRadius : fleeEnterRadius;

        var zombies = NPCManager.Instance.Zombies;
        Vector3 myPos = transform.position;
        float r2 = radius * radius;

        for (int i = 0; i < zombies.Count; i++)
        {
            var z = zombies[i];
            if (z == null || !z.gameObject.activeInHierarchy) continue;

            if ((z.transform.position - myPos).sqrMagnitude <= r2)
                return true;
        }

        return false;
    }

    // 외부 감염
    public void Infect()
    {
        PoolManager.Instance.Despawn("Citizen", gameObject);
    }
}
