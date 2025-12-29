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
 
    [SerializeField]
    protected NavMeshAgent agent;
    public NavMeshAgent Agent => agent;
 
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
        
        
        // ===== Swim =====
    protected bool isSwimming = false;
    int waterAreaMask;

    [Header("Debug")]
    public bool debugLog = false;

    // ===== Door Flee =====
    protected Door fleeDoor;
    protected bool fleeingToDoor = false;
    enum FleeStrategy
{
    None,
    Door,
    Direction
}

FleeStrategy fleeStrategy = FleeStrategy.None;
    
    // 🔥 디버그용: 문 선택 실패 이유
    [System.NonSerialized]
    public string doorSelectFailReason = "";

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

        waterAreaMask = 1 << NavMesh.GetAreaFromName("Water");
    }

    protected virtual void Start()
    {
        // 🔹 "행동 시작"은 Start에서
        if (!agent.enabled)
            agent.enabled = true;

        //ChangeState(State.Wander);
        //SetNewWanderTarget();
    }
   
    [SerializeField] protected float thinkInterval = 0.2f; // 초당 5회
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
        // Shoot 같은 1회성도 포함해서, 같으면 재생 안 하게
        if (currentAnim == animName)
            return;

        currentAnim = animName;
        animatedMesh.Play(animName);
    }
    protected void PlayMoveAnim(string landAnim)
    {
        if (isSwimming)
            PlayAnim("StickMan_Swim");
        else
            PlayAnim(landAnim);
    }
    bool IsOnWater()
    {
        if (!agent.isOnNavMesh) return false;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 0.2f, NavMesh.AllAreas))
        {
            int area = hit.mask;
            return (area & (1 << NavMesh.GetAreaFromName("Water"))) != 0;
        }
        return false;
    }

    // ---------------- STATE CHANGE ----------------
    protected void ChangeState(State newState)
    {
        if (isCommandLocked)
            return;   // ⭐ 이거 없으면 구조적으로 절대 해결 안 됨
   

        state = newState;

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
                idleTimer = Random.Range(idleMin, idleMax); // ⭐ 여기서 세팅
                // 🔥 Flee 상태에서 벗어날 때 Door Flee 관련 변수 리셋
                passingDoor = false;
                doorStuckTimer = 0f;
                fleeDoor = null;
                fleeStrategy = FleeStrategy.None;
                PlayMoveAnim("StickMan_Idle");
                break;

            case State.Wander:
                agent.isStopped = false;
                agent.speed = wanderSpeed;
                PlayMoveAnim("StickMan_Walk");
                // 🔥 Flee 상태에서 벗어날 때 Door Flee 관련 변수 리셋
                passingDoor = false;
                doorStuckTimer = 0f;
                fleeDoor = null;
                fleeStrategy = FleeStrategy.None;
                SetNewWanderTarget();
                break;

            case State.Flee:
                agent.isStopped = false;
                agent.speed = fleeSpeed;
                PlayMoveAnim("StickMan_Run");
                // 🔥 Door Flee 관련 변수 리셋
                passingDoor = false;
                doorStuckTimer = 0f;
                // 🔥 문 우선 시도
                SelectFleeStrategy();
                
                TrySetNewFleeTarget();
                break;
        }
    }
    void SelectFleeStrategy()
{
    // 1️⃣ 문 전략 시도
    if (TrySelectDoorFlee())
    {
        fleeStrategy = FleeStrategy.Door;
        return;
    }

    // 2️⃣ 방향 전략
    if (TrySetNewFleeTarget())
    {
        fleeStrategy = FleeStrategy.Direction;
        return;
    }

    fleeStrategy = FleeStrategy.None;
}

    bool CheckSwimming()
    {
        if (!agent.isOnNavMesh) return false;

        if (NavMesh.SamplePosition(
            transform.position,
            out var hit,
            0.3f,
            NavMesh.AllAreas))
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
            case State.Idle:
                PlayMoveAnim("StickMan_Idle");
                break;

            case State.Wander:
                PlayMoveAnim("StickMan_Walk");
                break;

            case State.Flee:
                PlayMoveAnim("StickMan_Run");
                break;
        }
    }

    // ---------------- WANDER ----------------
    protected void UpdateWander()
    {
        // Idle 상태일 때
        
           
        

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
              
                idleTimer = Random.Range(idleMin, idleMax);
                ChangeState(State.Idle);   // ⭐ 핵심
                Log($"Idle → Enter ({idleTimer:F2} sec)");
                return;
            }

            SetNewWanderTarget();
        }

        agent.SetDestination(wanderTarget);
        agent.isStopped = false;

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            PlayMoveAnim("StickMan_Walk");
        }

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
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        PlayMoveAnim("StickMan_Idle");

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
      
        // 1️⃣ 좀비 없으면 그때만 종료
        if (!DetectZombie())
        {
            ChangeState(State.Wander);
            return;
        }
        
        // 🔥 방향 전략일 때도 주기적으로 문 전략을 다시 시도
        if (fleeStrategy == FleeStrategy.Direction)
        {
            // 문 전략 재평가 (더 나은 문이 있을 수 있음)
            if (TrySelectDoorFlee())
            {
                fleeStrategy = FleeStrategy.Door;
            }
        }
        
    switch (fleeStrategy)
        {
            case FleeStrategy.Door:
                UpdateDoorFlee();
                break;

            case FleeStrategy.Direction:
                UpdateDirectionFlee();
                break;

            default:
                SelectFleeStrategy();
                break;
        }
        // 2️⃣ 목적지 거의 도착했으면 즉시 다음 도망
      
    }
 bool passingDoor = false;
float doorStuckTimer = 0f;
const float doorStuckTimeout = 2f; // 2초 동안 움직이지 않으면 재시도

void UpdateDoorFlee()
{
    if (fleeDoor == null)
    {
        passingDoor = false;
        doorStuckTimer = 0f;
        SelectFleeStrategy();
        return;
    }

    // 🔥 접근점까지의 경로가 제대로 설정되어 있는지 확인
    Vector3 approachPoint = fleeDoor.GetApproachPoint();
    float distToApproach = Vector3.Distance(transform.position, approachPoint);
    
    // 🔥 경로가 없거나 목적지가 접근점이 아니면 재설정
    if (!agent.hasPath || Vector3.Distance(agent.destination, approachPoint) > 0.5f)
    {
        NavMeshPath testPath = new NavMeshPath();
        if (agent.CalculatePath(approachPoint, testPath))
        {
            // PathComplete 또는 PathPartial 허용
            if (testPath.status != NavMeshPathStatus.PathInvalid)
            {
                agent.SetDestination(approachPoint);
                agent.isStopped = false;
                doorStuckTimer = 0f;
            }
        }
    }

    // 🔥 경로 상태 체크
    bool pathBlocked = !agent.hasPath || 
                       agent.pathStatus == NavMeshPathStatus.PathInvalid;

    // 🔥 제자리 걸음 감지 (속도가 거의 없고 remainingDistance가 일정 시간 동안 변하지 않음)
    bool isStuck = agent.velocity.sqrMagnitude < 0.01f && agent.remainingDistance > 0.5f && distToApproach > 0.5f;
    
    if (pathBlocked || isStuck)
    {
        doorStuckTimer += Time.deltaTime;
        
        // 🔥 경로가 막혀있거나 제자리 걸음이면 재시도
        float timeout = pathBlocked ? 0.5f : doorStuckTimeout;
        
        if (doorStuckTimer >= timeout)
        {
            // 🔥 접근점으로 경로 재설정 시도
            NavMeshPath retryPath = new NavMeshPath();
            if (agent.CalculatePath(approachPoint, retryPath))
            {
                // PathComplete 또는 PathPartial 허용
                if (retryPath.status != NavMeshPathStatus.PathInvalid)
                {
                    agent.SetDestination(approachPoint);
                    agent.isStopped = false;
                    doorStuckTimer = 0f;
                }
                else
                {
                    // 경로를 찾을 수 없으면 방향 전략으로 전환
                    passingDoor = false;
                    doorStuckTimer = 0f;
                    fleeDoor = null;
                    fleeStrategy = FleeStrategy.None;
                    SelectFleeStrategy();
                    return;
                }
            }
            else
            {
                // 경로 계산 실패 시 방향 전략으로 전환
                passingDoor = false;
                doorStuckTimer = 0f;
                fleeDoor = null;
                fleeStrategy = FleeStrategy.None;
                SelectFleeStrategy();
                return;
            }
        }
    }
    else
    {
        doorStuckTimer = 0f; // 움직이고 있으면 타이머 리셋
    }

    // 🔥 에이전트가 멈춰있으면 강제로 재개
    if (agent.isStopped && !passingDoor)
    {
        agent.isStopped = false;
    }

    // 1️⃣ 문 앞 도착 (더 가까운 거리로 체크하고, 실제로 도착했는지 확인)
    if (!passingDoor)
    {
        float distToDoor = Vector3.Distance(transform.position, fleeDoor.GetApproachPoint());
        float distToDoorPos = Vector3.Distance(transform.position, fleeDoor.transform.position);
        
        // 🔥 문 접근점에 충분히 가까웠거나, 경로가 완료되었고 거리가 충분히 가까울 때
        bool reachedApproachPoint = distToDoor <= 0.6f || (!agent.pathPending && agent.remainingDistance <= 0.5f && distToDoor <= 1.0f);
        
        // 🔥 문에 가까이 가면 문이 열리도록 함 (Door의 CheckCitizenIntent가 처리)
        // 문이 열릴 때까지 기다림
        if (reachedApproachPoint || distToDoorPos <= 1.0f)
        {
            // 🔥 문이 통과 가능한지 확인
            if (fleeDoor.IsPassable())
            {
                // 문이 열려있으면 통과 지점으로 이동
                Vector3 pass = fleeDoor.GetPassThroughPoint(transform.position);
                NavMeshPath passPath = new NavMeshPath();
                
                // 통과 지점까지 경로 계산
                if (agent.CalculatePath(pass, passPath) && 
                    passPath.status == NavMeshPathStatus.PathComplete)
                {
                    if (NavMesh.SamplePosition(pass, out var hit, 2f, NavMesh.AllAreas))
                    {
                        passingDoor = true;
                        doorStuckTimer = 0f;
                        agent.SetDestination(hit.position);
                        agent.isStopped = false;
                    }
                    else
                    {
                        // 통과 지점을 찾을 수 없으면 방향 전략으로 전환
                        passingDoor = false;
                        doorStuckTimer = 0f;
                        fleeDoor = null;
                        SelectFleeStrategy();
                    }
                }
                else
                {
                    // 통과 경로를 찾을 수 없으면 잠시 대기 (문이 더 열릴 수 있음)
                    // 또는 방향 전략으로 전환
                    if (distToDoorPos > 0.8f)
                    {
                        // 문에서 너무 멀리 떨어져 있으면 방향 전략으로 전환
                        passingDoor = false;
                        doorStuckTimer = 0f;
                        fleeDoor = null;
                        SelectFleeStrategy();
                    }
                    else
                    {
                        // 문 앞에서 대기 (문이 더 열릴 때까지)
                        agent.isStopped = true;
                    }
                }
            }
            else
            {
                // 🔥 문이 아직 닫혀있으면 문 앞에서 대기
                // Door의 CheckCitizenIntent가 문을 열어줄 것임
                if (distToDoorPos <= 1.0f)
                {
                    // 문 앞에서 대기
                    agent.isStopped = true;
                    // 접근점으로 계속 이동 시도 (문이 열릴 수 있도록)
                    if (distToDoor > 0.3f)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(fleeDoor.GetApproachPoint());
                    }
                }
            }
            return;
        }
    }

    // 2️⃣ 문 완전 통과
    if (passingDoor)
    {
        float distToPassPoint = Vector3.Distance(transform.position, fleeDoor.GetPassThroughPoint(transform.position));
        
        // 통과 지점에 충분히 가까웠거나, 경로가 완료되었고 거리가 충분히 가까울 때
        if (distToPassPoint <= 0.8f || (!agent.pathPending && agent.remainingDistance <= 0.5f && distToPassPoint <= 1.2f))
        {
            // 🔥 이제 진짜 공간 전환 완료
            fleeDoor = null;
            passingDoor = false;
            doorStuckTimer = 0f;

            // 새 공간 기준으로 다시 도망 판단
            SelectFleeStrategy();
        }
    }
}

void UpdateDirectionFlee()
{
    // 🔥 매 프레임마다 문 전략을 먼저 확인 (문이 있으면 무조건 문으로!)
    if (TrySelectDoorFlee())
    {
        fleeStrategy = FleeStrategy.Door;
        fleeRetargetTimer = 0f;
        return;
    }
    
    // 문 전략 실패 시에만 방향 전략
    fleeRetargetTimer += Time.deltaTime;
    if (fleeRetargetTimer >= fleeRetargetInterval)
    {
        fleeRetargetTimer = 0f;
        TrySetNewFleeTarget();
        return;
    }

    // 목적지 도착 체크
    if (!agent.pathPending && agent.remainingDistance <= 0.6f)
    {
        TrySetNewFleeTarget();
        fleeRetargetTimer = 0f;
        return;
    }

    // 혹시 경로가 없으면 강제 재설정
    if (!agent.hasPath)
    {
        TrySetNewFleeTarget();
        fleeRetargetTimer = 0f;
        return;
    }
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
            key = NPCManager.Instance.greenZombiePool; // 기본값
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
  #if UNITY_EDITOR
protected virtual void OnDrawGizmos()
{
    if (!Application.isPlaying) return;
    if (agent == null) return;

    // =============================
    // 1️⃣ 상태 텍스트
    // =============================
    Vector3 pos = transform.position + Vector3.up * 2.0f;

    string text = $"State: {state}\n";
    text += $"CmdLock: {isCommandLocked}\n";
    
    // 🔥 Flee 상태일 때 전략 정보 표시
    if (state == State.Flee)
    {
        text += $"Strategy: {fleeStrategy}\n";
        
        if (fleeStrategy == FleeStrategy.Door)
        {
            if (fleeDoor != null)
            {
                float distToDoor = Vector3.Distance(transform.position, fleeDoor.GetApproachPoint());
                text += $"Door: {fleeDoor.name}\n";
                text += $"Dist: {distToDoor:F1}m\n";
                text += $"Passing: {passingDoor}\n";
                
                // 🔥 경로 상태 표시
                if (agent.hasPath)
                {
                    text += $"Path: {agent.pathStatus}\n";
                    text += $"Remaining: {agent.remainingDistance:F1}m\n";
                    text += $"Velocity: {agent.velocity.magnitude:F2}\n";
                }
                else
                {
                    text += "Path: NO PATH!\n";
                }
                
                // 목적지와 접근점 거리 확인
                Vector3 approachPoint = fleeDoor.GetApproachPoint();
                float destDist = Vector3.Distance(agent.destination, approachPoint);
                if (destDist > 0.5f)
                {
                    text += $"Dest mismatch: {destDist:F1}m\n";
                }
            }
            else
            {
                text += "Door: NULL!\n";
            }
        }
        else if (fleeStrategy == FleeStrategy.Direction)
        {
            text += $"Reason: {doorSelectFailReason}\n";
            
            // 문 선택 실패 이유 상세 표시
            var doors = NPCManager.Instance?.Doors;
            if (doors != null && doors.Count > 0)
            {
                int doorCount = 0;
                int tooFar = 0;
                int noPath = 0;
                int pathInvalid = 0;
                int pathTooLong = 0;
                
                Vector3 myPos = transform.position;
                float maxDist = Mathf.Max(fleeDistance * 4f, 30f);
                
                foreach (var door in doors)
                {
                    if (door == null) continue;
                    doorCount++;
                    
                    Vector3 ap = door.GetApproachPoint();
                    float dist = Vector3.Distance(myPos, ap);
                    
                    if (dist > maxDist)
                    {
                        tooFar++;
                        continue;
                    }
                    
                    NavMeshPath path = new NavMeshPath();
                    if (!agent.CalculatePath(ap, path))
                    {
                        noPath++;
                        continue;
                    }
                    
                    if (path.status == NavMeshPathStatus.PathInvalid)
                    {
                        pathInvalid++;
                        continue;
                    }
                    
                    float pathLength = 0f;
                    if (path.corners.Length > 1)
                    {
                        for (int i = 0; i < path.corners.Length - 1; i++)
                        {
                            pathLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                        }
                        if (pathLength > dist * 5f)
                        {
                            pathTooLong++;
                        }
                    }
                }
                
                text += $"Doors: {doorCount}\n";
                if (tooFar > 0) text += $"TooFar: {tooFar}\n";
                if (noPath > 0) text += $"NoPath: {noPath}\n";
                if (pathInvalid > 0) text += $"Invalid: {pathInvalid}\n";
                if (pathTooLong > 0) text += $"TooLong: {pathTooLong}\n";
            }
            else
            {
                text += "Doors: 0\n";
            }
        }
        else
        {
            text += "Strategy: None\n";
        }
    }

    Handles.Label(pos, text);

    // =============================
    // 2️⃣ NavMesh Area 디버그 (UI 패키지 방식)
    // =============================
    if (agent.isOnNavMesh &&
        NavMesh.SamplePosition(
            transform.position,
            out var hit,
            0.3f,
            NavMesh.AllAreas))
    {
        int waterMask = 1 << NavMesh.GetAreaFromName("Water");
        bool isWater = (hit.mask & waterMask) != 0;

        // 색상
        Gizmos.color = isWater ? Color.blue : Color.green;

        // 발밑 포인트
        Gizmos.DrawSphere(hit.position, 0.25f);
        Gizmos.DrawLine(transform.position, hit.position);

        // 텍스트
        Handles.Label(
            hit.position + Vector3.up * 0.4f,
            isWater ? "AREA: WATER" : "AREA: LAND"
        );
    }

    // =============================
    // 3️⃣ 경로 시각화
    // =============================
    if (agent.hasPath && agent.path.corners.Length > 0)
    {
        // 상태에 따라 색상 변경
        Color pathColor = state == State.Flee ? Color.red : 
                         state == State.Wander ? Color.green : 
                         Color.yellow;
        
        // 경로 선 그리기
        Gizmos.color = pathColor;
        Vector3[] corners = agent.path.corners;
        
        // 현재 위치에서 첫 번째 코너까지
        Gizmos.DrawLine(transform.position, corners[0]);
        
        // 코너들 사이 선 그리기
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
        
        // 목적지 표시
        Gizmos.color = pathColor;
        Gizmos.DrawSphere(agent.destination, 0.3f);
        
        // 각 코너에 작은 구체 표시
        Gizmos.color = pathColor * 0.7f;
        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawSphere(corners[i], 0.15f);
        }
        
        // 문 전략일 때 문 위치 표시
        if (fleeStrategy == FleeStrategy.Door && fleeDoor != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 doorPos = fleeDoor.transform.position;
            Gizmos.DrawWireSphere(doorPos, 0.5f);
            Gizmos.DrawLine(transform.position, doorPos);
            
            // 접근점 표시
            Vector3 approachPoint = fleeDoor.GetApproachPoint();
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(approachPoint, 0.25f);
        }
    }
    else if (agent.destination != Vector3.zero)
    {
        // 경로가 없지만 목적지가 있을 때 (경로 계산 중이거나 막혀있을 때)
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, agent.destination);
        Gizmos.DrawWireSphere(agent.destination, 0.3f);
    }
}
#endif

bool IsOnWater(NavMeshHit hit)
{
    int waterMask = 1 << NavMesh.GetAreaFromName("Water");
    return (hit.mask & waterMask) != 0;
}
void DebugNavMeshArea_UI()
{
    if (!agent.isOnNavMesh)
    {
        Debug.Log("[NavMesh] Agent not on NavMesh");
        return;
    }

    if (NavMesh.SamplePosition(
        transform.position,
        out var hit,
        0.3f,
        NavMesh.AllAreas))
    {
        int waterMask = 1 << NavMesh.GetAreaFromName("Water");

        bool isWater = (hit.mask & waterMask) != 0;

        Debug.Log(
            $"[NavMesh UI] " +
            $"IsWater={isWater}, " +
            $"HitMask={hit.mask}, " +
            $"WaterMask={waterMask}"
        );
    }
}

    public virtual void RunTo(Vector3 target)
    {
        Debug.Log($"[RunTo] {name} → target={target}");

        isCommandLocked = true;
        agent.speed = fleeSpeed; // 임시 가속

        agent.isStopped = false;
        agent.SetDestination(target);

        Debug.Log($"[RunTo] locked={isCommandLocked}, dest={agent.destination}");

        PlayMoveAnim("StickMan_Run");
    }
 
    public virtual void ResetForReuse()
    {
        isCommandLocked = false;

        idleTimer = 0f;
        timer = 0f;

        state = State.Idle; // 기본값
        currentAnim = "";
        
        // Door Flee 관련 변수 리셋
        fleeDoor = null;
        passingDoor = false;
        doorStuckTimer = 0f;
        fleeStrategy = FleeStrategy.None;
        
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

   protected bool TrySelectDoorFlee()
{
    var doors = NPCManager.Instance.Doors;
    if (doors == null || doors.Count == 0)
    {
        doorSelectFailReason = "No doors in manager";
        return false;
    }

    Vector3 myPos = transform.position;

    // 🔥 좀비 위치 계산 (도망 방향 결정용)
    var zombies = NPCManager.Instance.Zombies;
    Vector3 fleeDir = Vector3.zero;
    int zombieCount = 0;
    if (zombies != null && zombies.Count > 0)
    {
        foreach (var z in zombies)
        {
            if (z == null || !z.gameObject.activeInHierarchy) continue;
            float distToZombie = Vector3.Distance(myPos, z.transform.position);
            if (distToZombie <= fleeExitRadius)
            {
                Vector3 dir = (myPos - z.transform.position).normalized;
                fleeDir += dir;
                zombieCount++;
            }
        }
        if (zombieCount > 0)
            fleeDir /= zombieCount;
        fleeDir.Normalize();
    }

    Door best = null;
    float bestDist = float.MaxValue; // 🔥 거리 기반으로 먼저 선택 (가까운 문 우선)

    foreach (var door in doors)
    {
        if (door == null) continue;

        Vector3 ap = door.GetApproachPoint();
        float dist = Vector3.Distance(myPos, ap);

        // 🔥 거리 조건을 매우 관대하게 (문이 멀어도 선택 가능, 최대 30m)
        float maxDist = Mathf.Max(fleeDistance * 4f, 30f);
        if (dist > maxDist)
            continue;

        // 🔥 접근점까지의 경로 계산 (접근점은 문 앞이므로 obstacle 밖에 있어야 함)
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(ap, path))
            continue;
        
        // 🔥 PathComplete 또는 PathPartial 허용 (PathPartial도 허용 - obstacle 때문에 우회 가능)
        if (path.status == NavMeshPathStatus.PathInvalid)
            continue;

        // 🔥 경로 길이 체크를 매우 관대하게 (우회 경로도 허용)
        float pathLength = 0f;
        if (path.corners.Length > 1)
        {
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                pathLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            // 경로 길이 체크를 매우 관대하게 (5배까지 허용)
            float maxPathLength = dist * 5f;
            if (pathLength > maxPathLength)
                continue;
        }

        // 🔥 거리 기반으로 선택 (가까운 문 우선)
        // 거리가 같거나 비슷하면 방향과 효율성을 고려
        if (dist < bestDist - 0.5f) // 0.5m 이상 가까우면 무조건 선택
        {
            bestDist = dist;
            best = door;
        }
        else if (Mathf.Abs(dist - bestDist) <= 0.5f && best != null) // 거리가 비슷하면 (0.5m 이내)
        {
            // 방향과 효율성을 고려하여 선택
            float currentScore = 0f;
            float bestDoorScore = 0f;
            
            // 방향 점수
            if (zombieCount > 0 && fleeDir.sqrMagnitude > 0.1f)
            {
                Vector3 toDoor = (ap - myPos).normalized;
                float dot = Vector3.Dot(fleeDir, toDoor);
                currentScore += (dot + 1f) * 0.5f * 100f;
                
                Vector3 bestDoorAp = best.GetApproachPoint();
                Vector3 toBestDoor = (bestDoorAp - myPos).normalized;
                float bestDot = Vector3.Dot(fleeDir, toBestDoor);
                bestDoorScore += (bestDot + 1f) * 0.5f * 100f;
            }
            
            // 효율성 점수
            if (pathLength > 0)
            {
                float efficiency = dist / pathLength;
                currentScore += efficiency * 50f;
            }
            
            if (currentScore > bestDoorScore)
            {
                bestDist = dist;
                best = door;
            }
        }
    }

    if (best == null)
    {
        // 🔥 문을 찾지 못했지만, 문이 존재한다면 가장 가까운 문이라도 선택 시도
        Door closestDoor = null;
        float closestDist = float.MaxValue;
        int checkedDoors = 0;
        
        foreach (var door in doors)
        {
            if (door == null) continue;
            checkedDoors++;
            float dist = Vector3.Distance(myPos, door.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestDoor = door;
            }
        }
        
        // 가장 가까운 문이 50m 이내면 강제로 선택
        if (closestDoor != null && closestDist <= 50f)
        {
            best = closestDoor;
            doorSelectFailReason = $"Fallback: {closestDoor.name} ({closestDist:F1}m)";
        }
        else
        {
            doorSelectFailReason = $"All {checkedDoors} doors rejected (closest: {closestDist:F1}m)";
            return false;
        }
    }
    else
    {
        doorSelectFailReason = $"Selected: {best.name} (dist: {bestDist:F1}m)";
    }

    // 🔥 실제로 경로를 설정하고 유효성 재확인
    Vector3 bestAp = best.GetApproachPoint();
    
    // 🔥 경로 재계산 및 설정
    NavMeshPath finalPath = new NavMeshPath();
    if (agent.CalculatePath(bestAp, finalPath))
    {
        // PathComplete 또는 PathPartial 허용
        if (finalPath.status != NavMeshPathStatus.PathInvalid)
        {
            agent.SetDestination(bestAp);
            agent.isStopped = false; // 🔥 확실히 이동 시작
            fleeDoor = best;
            doorStuckTimer = 0f;
            doorSelectFailReason = $"Selected: {best.name}";
            return true;
        }
        else
        {
            doorSelectFailReason = $"Path invalid to {best.name}";
            return false;
        }
    }
    
    // 경로 계산 실패
    doorSelectFailReason = $"Cannot calculate path to {best.name}";
    return false;
}


    
    // ---------------- DEBUG LOG ----------------
    private void Log(string msg)
    {
        if (!debugLog) return;
        Debug.Log($"[Citizen {name}] {msg}");
    }
    

   
}
