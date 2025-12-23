using System.Collections.Generic;
using System.IO;
using UnityEditor;
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

    [Header("Merge")]
    public float mergeDistance = 1.5f;
    private bool isMerging = false;


    private NavMeshAgent agent;
    [SerializeField] private CitizenBase targetCitizen;
    private ZombieNavMesh targetZombie;
    private float retargetInterval = 0.2f;
    private float retargetTimer;
    private Vector3 lastTargetPos;

    [Header("Animation")]
    [SerializeField] private AnimatedMesh animatedMesh;

    // 현재 재생 중인 애니메이션 Trigger 이름
    private string currentAnim = "";

  

    public Faction faction = Faction.Green;
    public string zombiePoolKey;


    public int maxHP = 1;
    public int currentHP = 1;

    [Header("Barricade Attack")]
    public float barricadeAttackInterval = 0.6f; // 한 대 치는 간격
    private float barricadeAttackTimer = 0f;

    [SerializeField]
    private Breakable targetBarricade;
    
    // ===== Swim =====
    bool isSwimming = false;
    int waterAreaMask;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = true;
        agent.acceleration = 20f;
        agent.autoBraking = false;

        animatedMesh = GetComponentInChildren<AnimatedMesh>();
        //anim = GetComponentInChildren<Animator>();

           waterAreaMask = 1 << NavMesh.GetAreaFromName("Water");
    }
  
    public Vector3 GetHitPoint()
    {
        // 머리/몸 중앙쯤
        return transform.position + Vector3.up * 0.8f;
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
        //if (anim != null)
        //    anim.speed = stats.animSpeed;
        // MoveSpeed 업그레이드 적용 (핵심!)
        float multiplier = UpgradeManager.Instance.GetMoveSpeedMultiplier();
        walkSpeed *= multiplier;
        runSpeed *= multiplier;

        // HP 적용
        maxHP = stats.maxHP;
        currentHP = maxHP;
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
    [SerializeField] float thinkInterval = 0.25f;
    float thinkTimer;
    void Update()
    {
   
        float dt = Time.deltaTime;

        // ⏱️ 타이머들은 매 프레임 누적 (애니/공격 타이밍용)
        retargetTimer += dt;
        barricadeAttackTimer += dt;

        // 🔒 Think 주기 제한
        thinkTimer -= dt;
        if (thinkTimer > 0f)
            return;

        thinkTimer = thinkInterval;

        Tick();
    }
    void Tick()
    {
        UpdateSwimmingState();
        // =======================
        // COMBAT MODE (좀비 vs 좀비)
        // =======================
        if (NPCManager.Instance.combatMode)
        {
            if (targetBarricade != null)
            {
                TryBreakBarricade();
                return;
            }

            if (retargetTimer >= retargetInterval)
            {
                retargetTimer = 0f;
                FindEnemyZombie();
            }

            TryMergeEnemy();
            return;
        }

        // =======================
        // NORMAL MODE (시민 추적)
        // =======================
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            FindNearestCitizen();
        }

        TryInfect(this.faction);

        if (targetBarricade != null)
            TryBreakBarricade();
    }
    //void Update()
    //{
    //    float dt = Time.deltaTime;

    //    retargetTimer += dt;
    //    barricadeAttackTimer += dt;


    //    // =======================
    //    // COMBAT MODE (좀비 vs 좀비)
    //    // =======================
    //    if (NPCManager.Instance.combatMode)
    //    {
    //        // 타겟 재탐색은 일정 간격으로만
    //        if (retargetTimer >= retargetInterval)
    //        {
    //            retargetTimer = 0f;
    //            FindEnemyZombie();
    //        }

    //        // 거리는 매 프레임 체크(머지 타이밍 때문에)
    //        TryMergeEnemy();
    //        return;
    //    }

    //    // =======================
    //    // NORMAL MODE (시민 추적)
    //    // =======================
    //    if (retargetTimer >= retargetInterval)
    //    {
    //        retargetTimer = 0f;
    //        FindNearestCitizen();
    //    }





    //    TryInfect(this.faction);

    //    if (targetBarricade != null)
    //        TryBreakBarricade();


    //}

    // ------- ANIMATION HELPER -------
    void PlayMoveAnim(string normalAnim)
    {
        if (isSwimming)
        {
            PlayAnim("Zombie_Swim");
        }
        else
        {
            PlayAnim(normalAnim);
        }
    }
    void PlayAnim(string animName)
    {
        if (currentAnim == animName) return;

        currentAnim = animName;
        animatedMesh.Play(animName);
    }

    // ---------------- FIND NEAREST CITIZEN ----------------
    void FindNearestCitizen()
    {
        CitizenBase nearest = null;
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
            PlayMoveAnim("Zombie_Idle");   // ← 여기서 Idle 재생
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
        // 경로 계산

        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(nearest.transform.position, path);


        // ⭐ Case 1: 시민까지 경로가 막혀 있다
        if (path.status != NavMeshPathStatus.PathComplete)
        {
            

            // 막힌 지점
            Vector3 blockedPoint = path.corners[path.corners.Length - 1];

            // 막힌 지점 주변 Breakable 검색
            Breakable blocker = FindBreakableBlockingPath(blockedPoint, 3f);

            if (blocker != null)
            {
               

                targetCitizen = null;
                targetBarricade = blocker;

                agent.SetDestination(blocker.transform.position);
                agent.isStopped = false;
                return;
            }

            // 가까운 Breakable이 없어도 기존 로직은 유지
            FindNearestBarricade();
            return;
        }
    }
    void UpdateSwimmingState()
    {
        bool nowSwimming = CheckSwimming();
        if (nowSwimming == isSwimming) return;

        isSwimming = nowSwimming;
        RefreshMoveAnimation();
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
        int waterMask = 1 << NavMesh.GetAreaFromName("Water");

        bool isWater = (hit.mask & waterMask) != 0;

        Debug.Log(
            $"[NavMesh Check] " +
            $"IsWater: {isWater}, " +
            $"HitMask: {hit.mask}, " +
            $"WaterMask: {waterMask}"
        );

        return isWater;
    }

    Debug.Log("[NavMesh Check] SamplePosition 실패");
    return false;
}
void RefreshMoveAnimation()
{
    // 멈춰있으면 Idle
    if (agent.isStopped || !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
    {
        PlayMoveAnim("Zombie_Idle");
        return;
    }

    // 속도 기준 Run/Walk (1.3배 런도 포함해서 판단)
    float runThreshold = (runSpeed * 0.5f) * (runSpeed * 0.5f);
    if (agent.velocity.sqrMagnitude >= runThreshold)
        PlayMoveAnim("Zombie_Run");
    else
        PlayMoveAnim("Zombie_Walk");
}
  


    void FindEnemyZombie()
    {
        // 적 진영 리스트 가져오기 (Green은 Purple+Yellow, Purple은 Green+Yellow, Yellow는 Green+Purple)
        List<ZombieNavMesh> enemies = new List<ZombieNavMesh>();
        
        if (faction == Faction.Green)
        {
            enemies.AddRange(NPCManager.Instance.PurpleZombies);
            enemies.AddRange(NPCManager.Instance.YellowZombies);
        }
        else if (faction == Faction.Purple)
        {
            enemies.AddRange(NPCManager.Instance.GreenZombies);
            enemies.AddRange(NPCManager.Instance.YellowZombies);
        }
        else if (faction == Faction.Yellow)
        {
            enemies.AddRange(NPCManager.Instance.GreenZombies);
            enemies.AddRange(NPCManager.Instance.PurpleZombies);
        }
        
        // 🔥 적이 없다 → 싸움 끝
        if (enemies.Count == 0)
        {
            targetZombie = null;
            isMerging = false;

            agent.isStopped = true;
            agent.ResetPath();   // ★ 목적지 초기화 필수
            PlayMoveAnim("Zombie_Idle");

            return;
        }


        ZombieNavMesh nearest = null;
        float minSqr = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;

            float sqr = (e.transform.position - transform.position).sqrMagnitude;
            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = e;
            }
        }

        //if (nearest != null)
        //{
        //    agent.isStopped = false;
        //    agent.speed = runSpeed * 1.3f; // 조금 빠르게
        //    agent.SetDestination(nearest.transform.position);
        //    targetZombie = nearest;
        //    isMerging = true;
        //    PlayAnim("Run");
        //}
        if (nearest == null) return;

        // ⭐ 경로 계산
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(nearest.transform.position, path);

        // ⭐ 경로가 막혀 있으면 → 장애물 처리
        if (path.status != NavMeshPathStatus.PathComplete)
        {
            Vector3 blockedPoint = path.corners[path.corners.Length - 1];
            Breakable blocker = FindBreakableBlockingPath(blockedPoint, 3f);

            if (blocker != null)
            {
                Debug.Log("경로막혀있습니다 내 색: "+faction);
                targetZombie = null;
                isMerging = false;
                
                // ⭐ 핵심
                targetBarricade = blocker;

                agent.isStopped = false;
                agent.ResetPath(); // 이전 좀비 목적지 제거
                agent.SetDestination(GetClosestPointOnBreakable(blocker));

                PlayMoveAnim("Zombie_Run");
                return; // ← 반드시 끊어야 함
            }
        }

        // ⭐ 정상 경로면 적 좀비 추적
        agent.isStopped = false;
        agent.speed = runSpeed * 1.3f;
        agent.SetDestination(nearest.transform.position);
        targetZombie = nearest;
        isMerging = true;
        PlayMoveAnim("Zombie_Run");
    }
    void TryMergeEnemy()
    {
        if (!isMerging) return;

        if (targetZombie == null || !targetZombie.gameObject.activeInHierarchy)
        {
            targetZombie = null;
            isMerging = false;
            agent.ResetPath();   // ★ 목적지 완전 초기화
            PlayMoveAnim("Zombie_Idle");
            return;
        }

        float sqr = (targetZombie.transform.position - transform.position).sqrMagnitude;

        if (sqr <= mergeDistance * mergeDistance)
        {

            ResolveCombat(targetZombie);
           
          
        }
    }
    // ---------------- SPEED / ANIMATION LOGIC ----------------
    void HandleSpeedBasedOnDistance(CitizenBase target)
    {
        float dist = Vector3.Distance(transform.position, target.transform.position);

        // 가까우면 Run
        if (dist <= chaseDistance)
        {
            agent.speed = runSpeed;
            PlayMoveAnim("Zombie_Run");
        }
        else
        {
            agent.speed = walkSpeed;
            PlayMoveAnim("Zombie_Walk");
        }
    }

    // ---------------- INFECT ----------------
    void TryInfect(Faction faction)
    {
        if (targetCitizen == null) return;
        if (!targetCitizen.gameObject.activeInHierarchy) return;

        float sqrDist = (targetCitizen.transform.position - transform.position).sqrMagnitude;
        float infectSqr = infectDistance * infectDistance;

        if (sqrDist > infectSqr) return;

        Vector3 spawnPos = targetCitizen.transform.position;

        // 감염 이펙트 생성
        string effectKey = faction switch
        {
            Faction.Green => "SpikyExplosionGreen",
            Faction.Purple => "SpikyExplosionPurple",
            Faction.Yellow => "SpikyExplosionYellow",
            _ => "SpikyExplosionGreen"
        };

        PoolManager.Instance.Spawn(effectKey, spawnPos, Quaternion.identity);
      
        //if (infectEffectPrefab != null)
        //{
        //    GameObject fx = Instantiate(infectEffectPrefab);
        //    fx.transform.position = spawnPos;
        //}

        // 시민 감염
        targetCitizen.Infect(this.faction);

       

        // 추적 초기화
        agent.isStopped = true;
        targetCitizen = null;
        agent.ResetPath();

        // 새 타겟 즉시 탐색
        FindNearestCitizen();
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;

        Debug.Log($"[Damage] {name} took {dmg} damage → HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Debug.Log($"[Death] {name} has died.");
            Die();
        }
    }

    void ResolveCombat(ZombieNavMesh other)
    {
        bool thisMutant = this.maxHP > 1;   // Mutant = HP2
        bool otherMutant = other.maxHP > 1;

        // --------------------------
        // Case 1) 둘 다 일반 좀비
        // --------------------------
        if (!thisMutant && !otherMutant)
        {
            // 즉사 교환
            Die();
            other.Die();
            return;
        }

        // --------------------------
        // Case 2) Mutant 포함 전투
        // --------------------------
        // 서로 체력 1씩 감소
        this.TakeDamage(1);
        other.TakeDamage(1);

        // ★HP 감소 후 Target 초기화 필요!
        if (!this.gameObject.activeInHierarchy)
        {
            // 내가 죽었으면 더 이상 Merge 못함
            return;
        }
        if (!other.gameObject.activeInHierarchy)
        {
            // 상대 죽으면 타겟 해제
            targetZombie = null;
            isMerging = false;
            agent.ResetPath();
            PlayMoveAnim("Zombie_Idle");
            return;
        }
    }
    public void Die()
    {
      
        string effectKey = faction switch
        {
            Faction.Green => "SpikyExplosionGreen",
            Faction.Purple => "SpikyExplosionPurple",
            Faction.Yellow => "SpikyExplosionYellow",
            _ => "SpikyExplosionGreen"
        };

        PoolManager.Instance.Spawn(effectKey, transform.position, Quaternion.identity);

        PoolManager.Instance.Despawn(zombiePoolKey, this.gameObject);
    }


    void FindNearestBarricade()
    {
        targetCitizen = null;

        var level = Game.Instance.CurrentLevel;
        if (level == null) return;

        var breakables = level.Breakables;

        float min = float.MaxValue;
        Breakable nearest = null;

        foreach (var b in breakables)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;

            float dist = (b.transform.position - transform.position).sqrMagnitude;
            if (dist < min)
            {
                min = dist;
                nearest = b;
            }
        }

        if (nearest != null)
        {
            targetBarricade = nearest;

            Vector3 targetPos = GetClosestPointOnBreakable(nearest);

            agent.isStopped = false;
            agent.SetDestination(targetPos);
        }
        else
        {
            targetBarricade = null;
        }
    }


    void TryBreakBarricade()
    {
        if (targetBarricade == null) return;

        // ⭐ 쿨타임 체크
        if (barricadeAttackTimer < barricadeAttackInterval)
            return;

        Vector3 attackPoint = GetClosestPointOnBreakable(targetBarricade);
        float dist = Vector3.Distance(transform.position, attackPoint);

        if (dist > infectDistance)
            return;

        barricadeAttackTimer = 0f; // 쿨타임 리셋

        // ---- 한 대 ----
        targetBarricade.TakeDamage(1);

        if (!targetBarricade.gameObject.activeInHierarchy)
        {
            targetBarricade = null;
            agent.ResetPath();
            if (NPCManager.Instance.combatMode)
                FindEnemyZombie();
            else
                FindNearestCitizen();
        }
    }

    Breakable FindBreakableBlockingPath(Vector3 blockedPoint, float radius = 2.5f)
    {
        var level = Game.Instance.CurrentLevel;
        if (level == null) return null;

        var breakables = level.Breakables;

        Breakable nearest = null;
        float minDist = float.MaxValue;

        foreach (var b in breakables)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(b.transform.position, blockedPoint);
            if (dist < radius && dist < minDist)
            {
                minDist = dist;
                nearest = b;
            }
        }

        return nearest;
    }


    Vector3 GetClosestPointOnBreakable(Breakable b)
    {
        Vector3 bPos = b.transform.position;
        Vector3 half = b.size * 0.5f;

        Vector3 z = transform.position;

        return new Vector3(
            Mathf.Clamp(z.x, bPos.x - half.x, bPos.x + half.x),
            z.y,
            Mathf.Clamp(z.z, bPos.z - half.z, bPos.z + half.z)
        );
    }
#if UNITY_EDITOR
void OnDrawGizmos()
{
    if (!Application.isPlaying) return;
    if (agent == null) return;

    // ===== NavMesh Area Debug =====
    if (agent.isOnNavMesh &&
        NavMesh.SamplePosition(
            transform.position,
            out var hit,
            0.3f,
            NavMesh.AllAreas))
    {
        int waterMask = 1 << NavMesh.GetAreaFromName("Water");
        bool isWater = (hit.mask & waterMask) != 0;

        // 🔵 Water / 🟢 Walkable
        Gizmos.color = isWater ? Color.blue : Color.green;

        // 발밑 히트 지점
        Gizmos.DrawSphere(hit.position, 0.25f);

        // 캐릭터 → 히트 지점
        Gizmos.DrawLine(transform.position, hit.position);

        // 상태 텍스트
        Handles.Label(
            hit.position + Vector3.up * 0.5f,
            isWater ? "WATER" : "LAND"
        );
    }

    // ===== 기존 타겟 디버그 =====
    Handles.Label(
        transform.position + Vector3.up * 2f,
        $"Faction: {faction}\n" +
        $"Citizen: {(targetCitizen ? targetCitizen.name : "None")}\n" +
        $"Zombie: {(targetZombie ? targetZombie.name : "None")}\n" +
        $"Barricade: {(targetBarricade ? targetBarricade.name : "None")}"
    );
}
#endif

}
