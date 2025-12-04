using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CitizenNavMesh : MonoBehaviour
{
    public enum State { Wander, Flee }

    // 전역 시민 리스트


    [Header("Speed")]
    public float wanderSpeed = 2f;
    public float fleeSpeed = 4f;

    [Header("Wander Settings")]
    public float wanderRadius = 6f;
    public float changeWanderInterval = 3f;

    [Header("Zombie Detection (Hysteresis)")]
    public float fleeEnterRadius = 4f;
    public float fleeExitRadius = 6f;
    public float fleeDistance = 7f;
    public LayerMask zombieLayer;

    private NavMeshAgent agent;
    private State state;
    private float timer;
    private Vector3 wanderTarget;
    private Vector3 fleeTarget;

    void OnEnable()
    {
        NPCManager.Instance.RegisterCitizen(this);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        ResetCitizenState();
    }

    void ResetCitizenState()
    {
        // TODO: 초기 wander 상태 리셋
        // 필요하면 상태머신 리셋 가능
    }

    void OnDisable()
    {
        NPCManager.Instance.UnregisterCitizen(this);
    }


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChangeState(State.Wander);
        SetNewWanderTarget();
    }

    void Update()
    {
        bool detected = DetectZombie();

        if (detected && state != State.Flee)
            ChangeState(State.Flee);
        else if (!detected && state != State.Wander)
            ChangeState(State.Wander);

        if (state == State.Wander) UpdateWander();
        else UpdateFlee();
    }

    // 외부(좀비)에서 감염시킬 때 호출
    public void Infect()
    {
        // 여기서 이펙트, 점수 증가 등 나중에 추가 가능
        PoolManager.Instance.Despawn("Citizen", gameObject);
    }

    // ------------------- STATE CHANGE -------------------
    void ChangeState(State newState)
    {
        state = newState;
        agent.speed = (state == State.Wander) ? wanderSpeed : fleeSpeed;

        if (state == State.Wander)
            SetNewWanderTarget();
        else
            SetNewFleeTarget();
    }

    // ------------------- WANDER -------------------
    void UpdateWander()
    {
        timer += Time.deltaTime;

        if (agent.remainingDistance <= 0.4f || timer >= changeWanderInterval)
        {
            timer = 0f;
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

    // ------------------- DETECT ZOMBIE -------------------
    bool DetectZombie()
    {
        float radius = (state == State.Flee) ? fleeExitRadius : fleeEnterRadius;
        return Physics.OverlapSphere(transform.position, radius, zombieLayer).Length > 0;
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
