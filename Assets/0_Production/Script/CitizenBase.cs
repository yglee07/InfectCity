using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public abstract class CitizenBase : MonoBehaviour
{
    public enum State { Idle, Wander, Flee }

    [Header("Idle Settings")]
    public float idleMin = 0.5f;
    public float idleMax = 2.0f;
    protected float idleTimer = 0f;

    [Header("Speed")]
    public float wanderSpeed = 2f;
    public float fleeSpeed = 4f;

    [Header("Wander Settings")]
    public float wanderRadius = 6f;
    public float changeWanderInterval = 3f;

    [Header("Zombie Detection")]
    public float fleeEnterRadius = 5f;
    public float fleeExitRadius = 7f;
    public float fleeDistance = 8f;

    [SerializeField] protected NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    protected State state;
    protected float timer;
    protected Vector3 wanderTarget;
    protected Vector3 fleeTarget;

    [SerializeField] protected Animator anim;
    protected string currentAnim = "";

    protected bool isCommandLocked;

    [SerializeField] protected AnimatedMesh animatedMesh;

    // ===== Swim =====
    protected bool isSwimming = false;
    int waterAreaMask;

    [Header("Debug")]
    public bool debugLog = false;

    // =========================================================
    // Door Assist (보너스 옵션: 방향 도망 중 "문이 안전하면" 문 너머로 보정)
    // - Door를 '목표'로 쓰지 않는다.
    // - Door 때문에 멈추거나 판단이 고정되는 상태가 절대 나오지 않는다.
    // - NavMeshPath 계산 / 코너 샘플링 전부 제거
    // =========================================================
    [Header("Door Assist (Flee Bonus)")]
    [SerializeField] private bool useDoorAssist = true;
    [SerializeField] private float doorAssistCheckInterval = 0.6f; // 문 후보 스캔 주기(시민당)
    [SerializeField] private float doorAssistMaxDist = 7.0f;       // "가까운 문"만 본다
    [SerializeField, Range(-1f, 1f)] private float doorAssistForwardDot = 0.35f; // 도망 방향 앞쪽(코사인)
    [SerializeField] private float doorApproachDangerRadius = 1.8f; // 문 앞 위험 반경
    [SerializeField] private float doorInsideDangerRadius = 2.2f;   // 문 안쪽(통과 지점) 위험 반경
    [SerializeField] private float doorPassPushDistance = 2.0f;     // 문 통과 후 추가로 밀어줄 거리

    private float nextDoorAssistTime = 0f;
    private Door lastAssistDoor = null; // 선택을 고정하지 않되, 디버그/안정감 용(선호 정도만)

    [SerializeField] private float fleeNoPathTimeout = 0.6f;
private float fleeNoPathTimer = 0f;

    // ===== Sound =====
    protected bool hasPlayedFleeScream = false;

    protected virtual void OnEnable()
    {
        ResetForReuse();

        NPCManager.Instance?.RegisterCitizen(this);

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        NPCManager.Instance?.UnregisterCitizen(this);
    }

    protected virtual void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (animatedMesh == null) animatedMesh = GetComponentInChildren<AnimatedMesh>();

        waterAreaMask = 1 << NavMesh.GetAreaFromName("Water");
    }

    protected virtual void Start()
    {
        if (!agent.enabled)
            agent.enabled = true;
    }

    [SerializeField] protected float thinkInterval = 0.2f;
    protected float thinkTimer = 0f;

    protected virtual void Update()
    {
        UpdateSwimmingState();

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
        if (animatedMesh == null)
        {
            animatedMesh = GetComponentInChildren<AnimatedMesh>();
            if (animatedMesh == null)
            {
                Debug.LogError($"[Citizen] AnimatedMesh missing on {name}");
                return;
            }
        }

        if (currentAnim == animName)
            return;

        currentAnim = animName;
        animatedMesh.Play(animName);
    }

    protected void PlayMoveAnim(string landAnim)
    {
        if (isSwimming) PlayAnim("StickMan_Swim");
        else PlayAnim(landAnim);
    }

    bool CheckSwimming()
    {
        if (!agent.isOnNavMesh) return false;

        if (NavMesh.SamplePosition(transform.position, out var hit, 0.3f, NavMesh.AllAreas))
        {
            return (hit.mask & waterAreaMask) != 0;
        }
        return false;
    }

    void UpdateSwimmingState()
    {
        bool nowSwimming = CheckSwimming();
        if (nowSwimming == isSwimming) return;

        isSwimming = nowSwimming;
        RefreshMoveAnimation();
    }

    void RefreshMoveAnimation()
    {
        switch (state)
        {
            case State.Idle: PlayMoveAnim("StickMan_Idle"); break;
            case State.Wander: PlayMoveAnim("StickMan_Walk"); break;
            case State.Flee: PlayMoveAnim("StickMan_Run"); break;
        }
    }

    // ---------------- STATE CHANGE ----------------
    protected void ChangeState(State newState)
    {
        if (isCommandLocked)
            return;

        state = newState;

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                idleTimer = Random.Range(idleMin, idleMax);
                PlayMoveAnim("StickMan_Idle");

                // Door Assist state reset
                lastAssistDoor = null;
                nextDoorAssistTime = 0f;
                break;

            case State.Wander:
                agent.isStopped = false;
                agent.speed = wanderSpeed;
                PlayMoveAnim("StickMan_Walk");

                // Door Assist state reset
                lastAssistDoor = null;
                nextDoorAssistTime = 0f;

                SetNewWanderTarget();
                break;

            case State.Flee:
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                PlayMoveAnim("StickMan_Run");

                if (!hasPlayedFleeScream)
                {
                    hasPlayedFleeScream = true;
                    Debug.Log("SoundManager: Citizen Flee Scream");
                    //if (Random.value < 0.35f) // 확률
                   // {
                       
                        SoundManager.Instance?
                            .PlayCitizenScream(transform.position);
                  //  } 
                }

                // Door Assist state reset
                lastAssistDoor = null;
                nextDoorAssistTime = 0f;

                // 방향 도망만 사용
                TrySetNewFleeTarget();
                break;
        }
    }

    // ---------------- WANDER ----------------
    protected void UpdateWander()
    {
        timer += Time.deltaTime;

        if (agent.remainingDistance <= 0.4f)
        {
            timer = 0f;
            SetNewWanderTarget();
            return;
        }

        if (timer >= changeWanderInterval)
        {
            timer = 0f;

            if (Random.value < 0.3f)
            {
                idleTimer = Random.Range(idleMin, idleMax);
                ChangeState(State.Idle);
                return;
            }

            SetNewWanderTarget();
        }

        agent.SetDestination(wanderTarget);
        agent.isStopped = false;

        if (agent.velocity.sqrMagnitude > 0.01f)
            PlayMoveAnim("StickMan_Walk");
    }

    private void SetNewWanderTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        Vector3 target = transform.position + new Vector3(rnd.x, 0, rnd.y);

        if (NavMesh.SamplePosition(target, out var hit, 2f, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
            agent.SetDestination(wanderTarget);
        }
    }

    protected virtual void UpdateIdle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        PlayMoveAnim("StickMan_Idle");

        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
            ChangeState(State.Wander);
    }

    // ---------------- FLEE ----------------
    [SerializeField] private float fleeRetargetInterval = 0.4f;
    private float fleeRetargetTimer = 0f;

    protected void UpdateFlee()
    {
        if (!DetectZombie())
        {
            ChangeState(State.Wander);
            return;
        }

        // 목적지 갱신(기존 구조 유지)
        fleeRetargetTimer += Time.deltaTime;
        if (fleeRetargetTimer >= fleeRetargetInterval)
        {
            fleeRetargetTimer = 0f;
            TrySetNewFleeTarget();
            return;
        }

        // 도착/경로 없음 보정
        if (!agent.pathPending && agent.remainingDistance <= 0.6f)
        {
            TrySetNewFleeTarget();
            fleeRetargetTimer = 0f;
            return;
        }

        if (!agent.hasPath)
        {
            TrySetNewFleeTarget();
            fleeRetargetTimer = 0f;
            return;
        }

        // === NavMesh 탈출 불가능 상태 감지 ===
bool noPath = !agent.hasPath && !agent.pathPending;
bool frozen = agent.velocity.sqrMagnitude < 0.001f;

if (noPath && frozen)
{
    fleeNoPathTimer += Time.deltaTime;

    if (fleeNoPathTimer >= fleeNoPathTimeout)
    {
        // 더 이상 도망칠 수 없음 → 상태 전환
        fleeNoPathTimer = 0f;

        // 선택 1: 그냥 Wander (추천)
        ChangeState(State.Wander);
        return;

        // 선택 2: Idle
        // ChangeState(State.Idle);
    }
}
else
{
    fleeNoPathTimer = 0f;
}
    }

    protected bool TrySetNewFleeTarget()
    {
        var zombies = NPCManager.Instance.Zombies;
        if (zombies == null || zombies.Count == 0)
            return false;

        Vector3 myPos = transform.position;

        // 1) 주변 좀비만 모아 fleeDir 계산 (기존 방식 유지)
        List<ZombieNavMesh> nearby = new();
        float exitR2 = fleeExitRadius * fleeExitRadius;

        for (int i = 0; i < zombies.Count; i++)
        {
            var z = zombies[i];
            if (z == null || !z.gameObject.activeInHierarchy) continue;

            float d2 = (z.transform.position - myPos).sqrMagnitude;
            if (d2 <= exitR2)
                nearby.Add(z);
        }

        if (nearby.Count == 0)
            return false;

        Vector3 fleeDir = Vector3.zero;
        for (int i = 0; i < nearby.Count; i++)
        {
            Vector3 dir = myPos - nearby[i].transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                fleeDir += dir.normalized;
        }

        if (fleeDir.sqrMagnitude < 0.001f)
            return false;

        fleeDir.Normalize();

        // 2) 후보 방향(기존 구조 유지)
        if (TrySetFleeTarget(myPos, fleeDir, nearby, 0) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, 45) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, -45) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, 90) ||
            TrySetFleeTarget(myPos, fleeDir, nearby, -90))
        {
            // 3) Door Assist: "도망 목표"가 아니라 fleeTarget을 '살짝 보정'
            if (useDoorAssist)
                TryApplyDoorAssist(ref fleeTarget, fleeDir, nearby);

            agent.SetDestination(fleeTarget);
            agent.isStopped = false;
            return true;
        }
Vector3 fallback = myPos + fleeDir * 2.5f;
if (NavMesh.SamplePosition(fallback, out var hit2, 2.5f, NavMesh.AllAreas))
{
    fleeTarget = hit2.position;
    agent.SetDestination(fleeTarget);
    agent.isStopped = false;
    return true;
}
        return false;
    }

    private bool TrySetFleeTarget(Vector3 myPos, Vector3 baseDir, List<ZombieNavMesh> nearby, float angleDeg)
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

        // (중요) 여기서는 기존처럼 path.CalculatePath + corner 검사까지 다 하면 비싸짐.
        // 500좀비 환경에서 시민 수가 많으면 여기서 폭발하니까,
        // "PathComplete만" 확인하거나, 아예 생략하는 쪽이 모바일에서 더 안전함.
        // 최소 유지 버전: PathComplete만 확인.
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        fleeTarget = hit.position;
        return true;
    }

    // =========================================================
    // Door Assist (핵심 변경)
    // - Door를 목표로 삼지 않음
    // - agent.isStopped 절대 안 건드림 (멈춤/멍때림 방지)
    // - NavMeshPath / corner 샘플링 제거
    // - "가까운 문" + "도망 방향 앞쪽" + "문이 현재 통과 가능" + "문 앞/안쪽 좀비 없을 때"만 적용
    // =========================================================
    private void TryApplyDoorAssist(
    ref Vector3 fleeDir,
    Vector3 myPos,
    List<ZombieNavMesh> nearbyZombies)
{
    if (Time.time < nextDoorAssistTime)
        return;

    nextDoorAssistTime = Time.time + doorAssistCheckInterval;

    var doors = NPCManager.Instance.Doors;
    if (doors == null || doors.Count == 0)
        return;

    Door closestDoor = null;
    float minDist2 = float.MaxValue;

    // 1) 가장 가까운 문 하나만 찾음
    for (int i = 0; i < doors.Count; i++)
    {
        var d = doors[i];
        if (d == null || !d.IsPassable()) continue;

        float d2 = (d.transform.position - myPos).sqrMagnitude;
        if (d2 < minDist2)
        {
            minDist2 = d2;
            closestDoor = d;
        }
    }

    if (closestDoor == null)
        return;

    // 2) 문 방향
    Vector3 ap = closestDoor.GetApproachPoint();
    Vector3 doorDir = (ap - myPos).normalized;

    // 도망 방향 앞쪽만
    if (Vector3.Dot(fleeDir, doorDir) < doorAssistForwardDot)
        return;

    // 3) 문 앞 위험하면 포기
    if (IsZombieNearPoint(nearbyZombies, ap, doorApproachDangerRadius))
        return;

    // 4) ⭐ NavMesh 연결성 검사 (중요)
    NavMeshPath path = new NavMeshPath();
    if (!agent.CalculatePath(ap, path) || path.status != NavMeshPathStatus.PathComplete)
        return;

    // 5) 방향만 보정 (절대 타겟 좌표를 바꾸지 않는다)
    fleeDir = (fleeDir + doorDir * 0.6f).normalized;
}


    private bool IsZombieNearPoint(List<ZombieNavMesh> nearbyZombies, Vector3 point, float radius)
    {
        float r2 = radius * radius;

        for (int i = 0; i < nearbyZombies.Count; i++)
        {
            var z = nearbyZombies[i];
            if (z == null || !z.gameObject.activeInHierarchy) continue;

            float d2 = (z.transform.position - point).sqrMagnitude;
            if (d2 <= r2)
                return true;
        }
        return false;
    }

    // ---------------- DETECT ZOMBIE ----------------
    protected bool DetectZombie()
    {
        float radius = (state == State.Flee) ? fleeExitRadius : fleeEnterRadius;
        float r2 = radius * radius;

        var zombies = NPCManager.Instance.Zombies;
        Vector3 myPos = transform.position;

        for (int i = 0; i < zombies.Count; i++)
        {
            var z = zombies[i];
            if (z == null || !z.gameObject.activeInHierarchy) continue;

            float dist2 = (z.transform.position - myPos).sqrMagnitude;
            if (dist2 <= r2)
                return true;
        }

        return false;
    }

    // ---------------- INFECT ----------------
    public virtual void Infect(Faction faction)
    {
        DespawnSelf();

        string key;
        if (faction == Faction.Green)
        {
            key = (Random.value < NPCManager.Instance.mutantChance)
                ? "Mutant"
                : NPCManager.Instance.greenZombiePool;
        }
        else if (faction == Faction.Purple)
        {
            key = NPCManager.Instance.purpleZombiePool;
        }
        else if (faction == Faction.Yellow)
        {
            key = NPCManager.Instance.yellowZombiePool;
        }
        else
        {
            key = NPCManager.Instance.greenZombiePool;
        }

        GameObject zombieObj = PoolManager.Instance.Spawn(key, transform.position, Quaternion.identity);
        ZombieNavMesh zombie = zombieObj.GetComponent<ZombieNavMesh>();
        zombie.faction = faction;

        NPCManager.Instance.AddInfectCount(faction);
    }

    protected virtual void DespawnSelf()
    {
        Debug.LogError("DespawnSelf() not overridden!");
    }

    public virtual void RunTo(Vector3 target)
    {
        isCommandLocked = true;
        agent.speed = fleeSpeed;
        agent.isStopped = false;
        agent.SetDestination(target);
        PlayMoveAnim("StickMan_Run");
    }

    public virtual void ResetForReuse()
    {
        isCommandLocked = false;

        idleTimer = 0f;
        timer = 0f;

        state = State.Idle;
        currentAnim = "";

        hasPlayedFleeScream = false; // ⭐ 추가


        // Door Assist reset
        lastAssistDoor = null;
        nextDoorAssistTime = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.speed = wanderSpeed;
        }

        if (anim != null)
        {
            anim.Rebind();
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
