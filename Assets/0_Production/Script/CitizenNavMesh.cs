using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CitizenNavMesh : MonoBehaviour
{
    public enum State { Wander, Flee }

    [Header("Idle Settings")]
    public float idleMin = 0.5f;
    public float idleMax = 2.0f;

    protected bool isIdle = false;
    protected float idleTimer = 0f;

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

    // 🔥 반드시 protected로 변경 (상속용)
    protected NavMeshAgent agent;
    protected Animator anim;

    protected State state;
    protected float timer;
    protected Vector3 wanderTarget;
    protected Vector3 fleeTarget;

    protected string currentAnim = "";

    [Header("Debug")]
    public bool debugLog = false;

    protected virtual void OnEnable()
    {
        NPCManager.Instance?.RegisterCitizen(this);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    protected virtual void OnDisable()
    {
        NPCManager.Instance?.UnregisterCitizen(this);
    }

    protected virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        ChangeState(State.Wander);
        SetNewWanderTarget();
    }

    // 🔥 반드시 virtual (CitizenShooter가 override하려면 필요)
    protected virtual void Update()
    {
        bool detected = DetectZombie();

        if (detected && state != State.Flee)
            ChangeState(State.Flee);
        else if (!detected && state != State.Wander)
            ChangeState(State.Wander);

        if (state == State.Wander) UpdateWander();
        else UpdateFlee();
    }

    // ======================
    //    Animation
    // ======================
    protected void PlayAnim(string trigger)
    {
        if (currentAnim == trigger) return;
        currentAnim = trigger;

        anim.ResetTrigger("Idle");
        anim.ResetTrigger("Walk");
        anim.ResetTrigger("Run");

        anim.SetTrigger(trigger);
    }

    // ======================
    //    State Change
    // ======================
    protected virtual void ChangeState(State newState)
    {
        Log($"State → {newState}");

        state = newState;
        isIdle = false;

        agent.speed = (newState == State.Wander) ? wanderSpeed : fleeSpeed;

        if (newState == State.Wander)
        {
            PlayAnim("Walk");
            SetNewWanderTarget();
        }
        else
        {
            PlayAnim("Run");
            SetNewFleeTarget();
        }
    }

    // ======================
    //    Wander Logic
    // ======================
    protected virtual void UpdateWander()
    {
        if (isIdle)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                Log("Idle → Exit");
                isIdle = false;
            }
            return;
        }

        timer += Time.deltaTime;
        if (timer >= changeWanderInterval)
        {
            timer = 0f;
            isIdle = true;
            idleTimer = Random.Range(idleMin, idleMax);
            Log($"Idle → Enter ({idleTimer:F2} sec)");
            return;
        }

        agent.SetDestination(wanderTarget);
    }

    protected void SetNewWanderTarget()
    {
        Vector3 random = Random.insideUnitSphere * wanderRadius;
        random.y = 0;

        wanderTarget = transform.position + random;

        if (NavMesh.SamplePosition(wanderTarget, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            wanderTarget = hit.position;

        Log($"WanderTarget = {wanderTarget}");
    }

    // ======================
    //    Flee logic
    // ======================
    protected virtual void UpdateFlee()
    {
        agent.SetDestination(fleeTarget);
    }

    protected void SetNewFleeTarget()
    {
        var zombies = NPCManager.Instance.Zombies;

        float min = float.MaxValue;
        Transform nearest = null;

        foreach (var z in zombies)
        {
            if (!z || !z.gameObject.activeInHierarchy) continue;

            float d = (z.transform.position - transform.position).sqrMagnitude;
            if (d < min)
            {
                min = d;
                nearest = z.transform;
            }
        }

        if (nearest == null)
        {
            Log("FleeTarget: No zombies found");
            return;
        }

        Vector3 away = (transform.position - nearest.position).normalized;
        Vector3 raw = transform.position + away * fleeDistance;
        raw += new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

        if (NavMesh.SamplePosition(raw, out var hit, 2f, NavMesh.AllAreas))
        {
            fleeTarget = hit.position;
            agent.SetDestination(fleeTarget);
            Log($"FleeTarget = {fleeTarget} (from zombie {nearest.name})");
        }
    }

    // ======================
    //    Detect Zombie
    // ======================
    protected bool DetectZombie()
    {
        float radius = (state == State.Flee) ? fleeExitRadius : fleeEnterRadius;
        float r2 = radius * radius;

        var zombies = NPCManager.Instance.Zombies;
        Vector3 myPos = transform.position;

        foreach (var z in zombies)
        {
            if (z == null || !z.gameObject.activeInHierarchy) continue;

            float dist2 = (z.transform.position - myPos).sqrMagnitude;

            if (dist2 <= r2)
            {
                Log($"Zombie detected → {z.name}, dist={Mathf.Sqrt(dist2):F2}");
                return true;
            }
        }

        return false;
    }

    // ======================
    //    External Infect
    // ======================
    public virtual void Infect(Faction faction)
    {
        Log($"INFECTED → Turns into {faction} zombie");
        PoolManager.Instance.Despawn("Citizen", gameObject);

        string key = NPCManager.Instance.greenZombiePool;
        if (faction == Faction.Green)
        {
            key = (Random.value < NPCManager.Instance.mutantChance)
                ? "Mutant"
                : NPCManager.Instance.greenZombiePool;
        }
        else
        {
            key = NPCManager.Instance.purpleZombiePool;
        }

        GameObject obj = PoolManager.Instance.Spawn(key, transform.position, Quaternion.identity);
        ZombieNavMesh zombie = obj.GetComponent<ZombieNavMesh>();
        zombie.faction = faction;

        NPCManager.Instance.AddInfectCount(faction);
    }

    protected void Log(string msg)
    {
        if (!debugLog) return;
        Debug.Log($"[Citizen {name}] {msg}");
    }
}
