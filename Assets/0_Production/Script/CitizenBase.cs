using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public abstract class CitizenBase: MonoBehaviour
{
    public enum State { Idle, Wander, Flee }

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
 

    [SerializeField]
    protected NavMeshAgent agent;
    protected State state;
    protected float timer;
    protected Vector3 wanderTarget;
    protected Vector3 fleeTarget;
    [SerializeField]
    protected Animator anim;

    protected string currentAnim = "";

    protected bool isCommandLocked;

    [SerializeField]
    protected AnimatedMesh animatedMesh;
       

    [Header("Debug")]
    public bool debugLog = false;

    protected virtual void OnEnable()
    {
        ResetForReuse();

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

    protected virtual void Awake()
    {
        // 🔹 캐싱만 한다
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        if(animatedMesh == null)
            animatedMesh = GetComponentInChildren<AnimatedMesh>();
    }

    protected virtual void Start()
    {
        // 🔹 "행동 시작"은 Start에서
        if (!agent.enabled)
            agent.enabled = true;

        //ChangeState(State.Wander);
        //SetNewWanderTarget();
    }
    //protected virtual void Update()
    //{
    //    if(debugLog)
    //        Debug.Log("parent update");
    //    if (behaviorType == CitizenBehaviorType.Sleep)
    //    {
    //        agent.isStopped = true;
    //        agent.velocity = Vector3.zero;
    //        PlayAnim("Sleep");
    //        return;
    //    }
    //    bool detected = DetectZombie();

    //    Log($"Update | behavior={behaviorType} state={state} detected={detected}");

    //    // ============================================
    //    // ★ CitizenBehaviorType.Idle 전용 로직
    //    // ============================================
    //    if (behaviorType == CitizenBehaviorType.Idle)
    //    {
    //        if (!detected)
    //        {
    //            // 좀비 없으면 Idle 유지
    //            agent.isStopped = true;
    //            agent.velocity = Vector3.zero;
    //            PlayAnim("Idle");

    //            Log("Idle citizen: No zombie → staying idle");
    //            return;   // Wander/Idle 랜덤 로직 접근 금지
    //        }
    //        else
    //        {
    //            Log("Idle citizen: Zombie detected → switching to Flee");
    //            agent.isStopped = false;
    //            // 좀비 감지 → Flee 전환
    //            if (state != State.Flee)
    //            {
    //                ChangeState(State.Flee);
    //                Log("Force ChangeState(Flee)");
    //            }
    //        }
    //    }

    //    // ============================================
    //    // Normal 시민 + Idle 시민 공통 상태 전환
    //    // ============================================
    //    if (detected && state != State.Flee)
    //    {
    //        Log("Detected → ChangeState(Flee)");
    //        ChangeState(State.Flee);
    //    }
    //    else if (!detected && state != State.Wander)
    //    {
    //        Log("No zombie → ChangeState(Wander)");
    //        ChangeState(State.Wander);
    //    }

    //    // ============================================
    //    // 상태 실행
    //    // ============================================
    //    if (state == State.Wander)
    //    {

    //        UpdateWander();
    //    }
    //    else
    //    {
    //        UpdateFlee();
    //    }



    //}

    [SerializeField] protected float thinkInterval = 0.2f; // 초당 5회
    protected float thinkTimer = 0f;

    protected virtual void Update()
    {
        if (isCommandLocked)
            return;

        thinkTimer -= Time.deltaTime;
        if (thinkTimer > 0f)
            return;

        thinkTimer = thinkInterval;
        Tick();
    }

    protected abstract void Tick();


    // ---------------- ANIMATION ----------------
    protected virtual void PlayAnim(string animName)
    {
        if (debugLog)
            Debug.Log("[Shooter] PlayAnim(" + animName + ") called");

        // Shoot 같은 1회성도 포함해서, 같으면 재생 안 하게
        if (currentAnim == animName)
            return;

        currentAnim = animName;
        animatedMesh.Play(animName);
    }

    // ---------------- STATE CHANGE ----------------
    protected void ChangeState(State newState)
    {
        if (isCommandLocked)
            return;   // ⭐ 이거 없으면 구조적으로 절대 해결 안 됨
        if (state == newState) return;

        state = newState;

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                PlayAnim("StickMan_Idle");
                break;

            case State.Wander:
                agent.isStopped = false;
                agent.speed = wanderSpeed;
                PlayAnim("StickMan_Walk");
                SetNewWanderTarget();
                break;

            case State.Flee:
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                PlayAnim("StickMan_Run");
                SetNewWanderTarget();
                break;
        }
    }


    // ---------------- WANDER ----------------
    protected void UpdateWander()
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
                PlayAnim("StickMan_Walk");
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
                ChangeState(State.Idle);   // ⭐ 핵심
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

    protected virtual void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0f)
        {
            ChangeState(State.Wander);
        }
    }
    // ---------------- FLEE ----------------
    [SerializeField] private float fleeRetargetInterval = 0.4f;
    private float fleeRetargetTimer = 0f;

    private Vector3 debugFleeDir; // Gizmo용
    protected void UpdateFlee()
    {
        // ✅ 도망 성공 판정
        if (!agent.pathPending && agent.remainingDistance <= 0.4f)
        {
            ChangeState(State.Idle); // 또는 Wander
            return;
        }

        fleeRetargetTimer += Time.deltaTime;
        if (fleeRetargetTimer < fleeRetargetInterval)
            return;

        fleeRetargetTimer = 0f;

        // ❌ 실패하면 다시 시도
        bool success = TrySetNewFleeTarget();
        // 실패 시 아무것도 안 함 → 다음 주기에 재시도
    }

    protected bool TrySetNewFleeTarget()
    {
        var zombies = NPCManager.Instance.Zombies;
        if (zombies == null || zombies.Count == 0)
            return false;

        Vector3 myPos = transform.position;

        List<ZombieNavMesh> nearby = new();
        foreach (var z in zombies)
        {
            if (z == null || !z.gameObject.activeInHierarchy) continue;
            if (Vector3.Distance(myPos, z.transform.position) <= fleeExitRadius)
                nearby.Add(z);
        }

        if (nearby.Count == 0)
            return false;

        Vector3 fleeDir = Vector3.zero;
        foreach (var z in nearby)
        {
            Vector3 dir = myPos - z.transform.position;
            fleeDir += dir.normalized;
        }

        if (fleeDir.sqrMagnitude < 0.001f)
            return false;

        fleeDir.Normalize();

        return
            TrySetFleeTarget(myPos, fleeDir, nearby, 0) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, 45) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, -45) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, 90) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, -90);
    }


    private bool TrySetFleeTarget(
    Vector3 myPos,
    Vector3 baseDir,
    List<ZombieNavMesh> nearby,
    float angleDeg
)
    {
        Vector3 dir = Quaternion.Euler(0, angleDeg, 0) * baseDir;
        dir.Normalize();

        Vector3 targetPos = myPos + dir * fleeDistance;

        // NavMesh 위치 보정
        if (!NavMesh.SamplePosition(targetPos, out var hit, 2.5f, NavMesh.AllAreas))
            return false;

        // 방향 뒤집힘 방지
        Vector3 toSample = (hit.position - myPos).normalized;
        if (Vector3.Dot(dir, toSample) < 0.5f)
            return false;

        // 경로 계산
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        // ===============================
        // 경로 안전성 검사 (좀비 쪽으로 휘는지)
        // ===============================
        // ===============================
        // 경로 위험도 검사 (중간 경로가 좀비에 가까워지는지)
        // ===============================
        foreach (var corner in path.corners)
        {
            foreach (var z in nearby)
            {
                float startDist = Vector3.Distance(myPos, z.transform.position);
                float cornerDist = Vector3.Distance(corner, z.transform.position);

                // ⚠️ 경로 중간에서 좀비에게 더 가까워지면 폐기
                if (cornerDist < startDist - 0.2f)
                {
                    // 이 목적지는 "좀비를 피해 도망"이 아니라
                    // "좀비를 스쳐서 도망"이 됨 → 부자연
                    return false;
                }
            }
        }


        // ===============================
        // 최종 적용
        // ===============================
        fleeTarget = hit.position;
        agent.SetDestination(fleeTarget);
        return true;
    }



    // ---------------- DETECT ZOMBIE ----------------
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

       
        DespawnSelf();
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
    protected virtual void DespawnSelf()
    {
        // 기본 구현은 막아두거나 경고
        Debug.LogError("DespawnSelf() not overridden!");
    }


    private void OnDrawGizmosSelected()
    {
        if (agent == null) return;

        // 현재 목적지
        Gizmos.color = (state == State.Flee) ? Color.red : Color.green;
        Gizmos.DrawSphere(agent.destination, 1.0f);

        // 현재 위치 → 목적지 선
        Gizmos.DrawLine(transform.position, agent.destination);
    }
    protected virtual void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Vector3 pos = transform.position + Vector3.up * 2.0f;

        string text =
            $"State: {state}\n" +
              // ⭐ 추가
            $"Idle: {isIdle}\n" +
            $"CmdLock: {isCommandLocked}";

        Handles.Label(pos, text);
#endif
    }
    public virtual void RunTo(Vector3 target)
    {
        Debug.Log($"[RunTo] {name} → target={target}");

        isCommandLocked = true;
        agent.speed = fleeSpeed; // 임시 가속

        agent.isStopped = false;
        agent.SetDestination(target);

        Debug.Log($"[RunTo] locked={isCommandLocked}, dest={agent.destination}");

        PlayAnim("StickMan_Run");
    }

    public virtual void ResetForReuse()
    {
        isCommandLocked = false;
        isIdle = false;
        idleTimer = 0f;
        timer = 0f;

        state = State.Wander;
        currentAnim = "";
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.speed = wanderSpeed;
        }

        if (anim != null)
        {
            anim.Rebind();     // 🔥 핵심
            anim.Update(0f);
        }
    }
    // ---------------- DEBUG LOG ----------------
    private void Log(string msg)
    {
        if (!debugLog) return;
        Debug.Log($"[Citizen {name}] {msg}");
    }
    

}
