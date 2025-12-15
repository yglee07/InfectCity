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

    protected virtual void Start()
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
        if(debugLog)
            Debug.Log("parent update");
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

            UpdateWander();
        }
        else
        {
            UpdateFlee();
        }
     
          
        
    }



    // ---------------- ANIMATION ----------------
    protected virtual void PlayAnim(string trigger)
    {
        if (debugLog) Debug.Log("[Shooter] PlayAnim(" + trigger + ") called");


        if (currentAnim == trigger)
        {
       
            return;
        }

        currentAnim = trigger;

       

        anim.ResetTrigger("Idle");
        anim.ResetTrigger("Walk");
        anim.ResetTrigger("Run");
        anim.ResetTrigger("Shoot");




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
    [SerializeField] private float fleeRetargetInterval = 0.4f;
    private float fleeRetargetTimer = 0f;

    private Vector3 debugFleeDir; // Gizmo용
    private void UpdateFlee()
    {
        fleeRetargetTimer += Time.deltaTime;

        // 일정 시간마다 무조건 새 도망 목표 설정
        if (fleeRetargetTimer >= fleeRetargetInterval)
        {
            fleeRetargetTimer = 0f;
            SetNewFleeTarget();
        }
    }

    private void SetNewFleeTarget()
    {
        var zombies = NPCManager.Instance.Zombies;
        if (zombies == null || zombies.Count == 0)
            return;

        Vector3 myPos = transform.position;

        // ===============================
        // 1️⃣ fleeExitRadius 안의 좀비 필터
        // ===============================
        List<ZombieNavMesh> nearby = new List<ZombieNavMesh>();
        foreach (var z in zombies)
        {
            if (z == null || !z.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(myPos, z.transform.position);
            if (dist <= fleeExitRadius)
                nearby.Add(z);
        }

        if (nearby.Count == 0)
            return;

        // ===============================
        // 2️⃣ 평균 반대 방향 계산
        // ===============================
        Vector3 fleeDir = Vector3.zero;
        foreach (var z in nearby)
        {
            Vector3 dir = myPos - z.transform.position;
            float weight = 1f / Mathf.Max(dir.magnitude, 0.1f);
            fleeDir += dir.normalized * weight;
        }

        if (fleeDir.sqrMagnitude < 0.001f)
            return;

        fleeDir.Normalize();
        debugFleeDir = fleeDir;

        // ===============================
        // 3️⃣ 좌/우 회전 포함 시도 (정면 → 측면)
        // ===============================
        if (TrySetFleeTarget(myPos, fleeDir, nearby, 0)) return;
        if (TrySetFleeTarget(myPos, fleeDir, nearby, 45)) return;
        if (TrySetFleeTarget(myPos, fleeDir, nearby, -45)) return;
        if (TrySetFleeTarget(myPos, fleeDir, nearby, 90)) return;
        if (TrySetFleeTarget(myPos, fleeDir, nearby, -90)) return;

        // 전부 실패 → 이번 프레임 포기
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

    private void OnDrawGizmosSelected()
    {
        if (agent == null) return;

        // 현재 목적지
        Gizmos.color = (state == State.Flee) ? Color.red : Color.green;
        Gizmos.DrawSphere(agent.destination, 1.0f);

        // 현재 위치 → 목적지 선
        Gizmos.DrawLine(transform.position, agent.destination);
    }

    // ---------------- DEBUG LOG ----------------
    private void Log(string msg)
    {
        if (!debugLog) return;
        Debug.Log($"[Citizen {name}] {msg}");
    }
}
