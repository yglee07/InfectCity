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

    [SerializeField]
    protected NavMeshAgent agent;
    protected State state;
    protected float timer;
    protected Vector3 wanderTarget;
    protected Vector3 fleeTarget;
    protected Animator anim;

    protected string currentAnim = "";

    public enum CitizenBehaviorType
    {
        Normal,   // 기존 이동 패턴
        Idle      // 움직이지 않는 시민
    }
    public CitizenBehaviorType behaviorType = CitizenBehaviorType.Normal;

    [Header("Debug")]
    public bool debugLog = false;

    private void OnEnable()
    {
        NPCManager.Instance?.RegisterCitizen(this);

        if (agent == null)
            agent=GetComponent<NavMeshAgent>();



        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        NPCManager.Instance?.UnregisterCitizen(this);
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();


        if (!agent.enabled)
            agent.enabled = true;  // ⭐ 안전장치 (Spawn보다 Start가 먼저 실행될 때 대비)
        ChangeState(State.Wander);
        SetNewWanderTarget();
    }

    protected virtual void Update()
    {
        bool detected = DetectZombie();

        Log($"Update | behavior={behaviorType} state={state} detected={detected}");

        // ============================================
        // ★ CitizenBehaviorType.Idle 전용 로직
        // ============================================
        if (behaviorType == CitizenBehaviorType.Idle)
        {
            if (!detected)
            {
                // 좀비 없으면 Idle 유지
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                PlayAnim("Idle");

                Log("Idle citizen: No zombie → staying idle");
                return;   // Wander/Idle 랜덤 로직 접근 금지
            }
            else
            {
                Log("Idle citizen: Zombie detected → switching to Flee");
                agent.isStopped = false;
                // 좀비 감지 → Flee 전환
                if (state != State.Flee)
                {
                    ChangeState(State.Flee);
                    Log("Force ChangeState(Flee)");
                }
            }
        }

        // ============================================
        // Normal 시민 + Idle 시민 공통 상태 전환
        // ============================================
        if (detected && state != State.Flee)
        {
            Log("Detected → ChangeState(Flee)");
            ChangeState(State.Flee);
        }
        else if (!detected && state != State.Wander)
        {
            Log("No zombie → ChangeState(Wander)");
            ChangeState(State.Wander);
        }

        // ============================================
        // 상태 실행
        // ============================================
        if (state == State.Wander)
        {
            Log("Run UpdateWander()");
            UpdateWander();
        }
        else
        {
            Log("Run UpdateFlee()");
            UpdateFlee();
        }
    }



    // ---------------- ANIMATION ----------------
    protected void PlayAnim(string trigger)
    {
        if (currentAnim == trigger) return;

        currentAnim = trigger;

        anim.ResetTrigger("Idle");
        anim.ResetTrigger("Walk");
        anim.ResetTrigger("Run");
        anim.ResetTrigger("Shoot");  // ⭐ 여기도 추가!

        anim.SetTrigger(trigger);
    }

    // ---------------- STATE CHANGE ----------------
    private void ChangeState(State newState)
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

    // ---------------- WANDER ----------------
    private void UpdateWander()
    {
        // Idle 상태일 때
        if (isIdle)
        {
            agent.SetDestination(transform.position);

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                Log("Idle → Exit");
                isIdle = false;
                SetNewWanderTarget();     // ⭐ 복구됨
                PlayAnim("Walk");
            }
            return;
        }

        timer += Time.deltaTime;

        // ⭐ Wander 도중 목적지 도착 체크
        if (agent.remainingDistance <= 0.4f)
        {
            Log("Reached wander target → Set new wander target");

            timer = 0f;
            SetNewWanderTarget();
            return;
        }

        // ⭐ 정기적으로 Idle 상태 진입
        if (timer >= changeWanderInterval)
        {
            timer = 0f;

            if (Random.value < 0.3f)
            {
                isIdle = true;
                idleTimer = Random.Range(idleMin, idleMax);
                PlayAnim("Idle");
                Log($"Idle → Enter ({idleTimer:F2} sec)");
                return;
            }

            SetNewWanderTarget();
        }

        agent.SetDestination(wanderTarget);
    }

    private void SetNewWanderTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        Vector3 target = transform.position + new Vector3(rnd.x, 0, rnd.y);

        if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
            agent.SetDestination(wanderTarget);
            Log($"WanderTarget = {wanderTarget}");
        }
    }

    // ---------------- FLEE ----------------
    private void UpdateFlee()
    {
        if (agent.remainingDistance <= 0.6f)
            SetNewFleeTarget();
    }

    private void SetNewFleeTarget()
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
            Log($"FleeTarget = {fleeTarget} (from zombie {nearest.name})");
        }
    }

    // ---------------- DETECT ZOMBIE ----------------
    private bool DetectZombie()
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
                Log($"DetectZombie → {z.name}, dist={Mathf.Sqrt(dist2):F2}");
                return true;
            }
        }

        return false;
    }

    // ---------------- INFECT ----------------
    public virtual void Infect(Faction faction)
    {
        Log($"INFECTED → Turns into {faction} zombie");

        PoolManager.Instance.Despawn("Citizen", gameObject);

        string key;

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

        GameObject zombieObj = PoolManager.Instance.Spawn(key, transform.position, Quaternion.identity);
        ZombieNavMesh zombie = zombieObj.GetComponent<ZombieNavMesh>();
        zombie.faction = faction;

        NPCManager.Instance.AddInfectCount(faction);
    }

    // ---------------- DEBUG LOG ----------------
    private void Log(string msg)
    {
        if (!debugLog) return;
        Debug.Log($"[Citizen {name}] {msg}");
    }
}
