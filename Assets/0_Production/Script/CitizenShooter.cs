using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class CitizenShooter : CitizenBase
{
    public enum ShooterMode { Stationary, Mobile }
    public ShooterMode mode = ShooterMode.Mobile;

    [Header("Shooter Settings")]
    public float shootRange = 6f;
    public float shootInterval = 1.2f;
    public int damage = 1;
    [SerializeField]
    private float shootTimer = 0f;

    [Header("Vision")]
    public float viewAngle = 90f;

#if UNITY_EDITOR
[SerializeField, TextArea(2, 6)]
private string debugReason = "";
#endif
    private ZombieNavMesh currentTarget;
    private ZombieNavMesh lastTarget;
    private bool isShooting = false;
    private FieldOfView fieldOfView;

    [Header("Muzzle")]
    public Transform muzzlePoint;
    [Header("Hit FX")]
    public string hitFxPoolKey = "RoundHitYellow";
    public string muzzlePoolKey = "BulletFatMuzzleFire";
    protected override void Awake()
    {
        base.Awake();

        fieldOfView = GetComponentInChildren<FieldOfView>();
        if (fieldOfView != null)
        {
            fieldOfView.BuildMesh(shootRange, viewAngle);
        }
        if (muzzlePoint == null)
             Debug.LogError("[CitizenShooter] MuzzlePoint not assigned!");
    }

    protected override void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (!agent.enabled)
            agent.enabled = true;

        agent.isStopped = true; // Wander 제거
        PlayAnim("StickMan_Idle");
       
        if (debugLog) Debug.Log("[Shooter] Start()");
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        isShooting = false;
    }
    protected override void Tick()
    {
        throw new System.NotImplementedException();
    }
    protected override void Update()
    {
        if (HandleCombat())
            return;

        agent.isStopped = true;
    }

    // ================= Combat =================
    protected bool HandleCombat()
    {
        if (isShooting &&
    (currentTarget == null || !currentTarget.gameObject.activeInHierarchy))
{
    isShooting = false;
}
        #if UNITY_EDITOR
debugReason = "";
#endif
        currentTarget = FindShootableTarget();

        if (currentTarget == null)
        {  
            fieldOfView?.SetAlert(false);
            #if UNITY_EDITOR
    debugReason = "No target (range/vision/none)";
#endif
            return false;
        }
fieldOfView?.SetAlert(true);
        if (debugLog) Debug.Log($"[Shooter] HandleCombat: 타겟 = {currentTarget.name}");

        agent.isStopped = true;

        // 회전
        Vector3 look = currentTarget.transform.position;
        look.y = transform.position.y;
        transform.LookAt(look);

        // 타겟 바뀌면 첫발 바로 쏘게 타이머 리셋
        if (currentTarget != lastTarget)
        {
            if (debugLog) Debug.Log("[Shooter] 새 타겟 → shootTimer 리셋");
         
            lastTarget = currentTarget;
          
        }

        shootTimer -= Time.deltaTime;
      #if UNITY_EDITOR
if (isShooting)
    debugReason = $"Aiming/Shooting (waiting anim event)\nshootTimer={shootTimer:F2}";
else if (shootTimer > 0f)
    debugReason = $"Cooldown\nshootTimer={shootTimer:F2}";
else
    debugReason = "Ready to shoot";
#endif

        if (shootTimer <= 0f && !isShooting)
        {
            if (debugLog) Debug.Log("[Shooter] ▶ PlayAnim(\"Shoot\")");
            isShooting = true;
     
            PlayAnim("StickMan_Shoot");
            //shootTimer = shootInterval;
        }

        return true;
    }

    // ================= Target 찾기 =================
    private ZombieNavMesh FindShootableTarget()
    {
        // 1차: NPCManager에서 가장 가까운 좀비
        ZombieNavMesh closest = NPCManager.Instance.FindClosestZombie(transform.position);

        if (closest != null)
        {
            float dist = Vector3.Distance(transform.position, closest.transform.position);
            if (debugLog) Debug.Log($"[Target] closest = {closest.name}, dist = {dist}");

            if (closest.gameObject.activeInHierarchy &&
                dist <= shootRange &&
                IsInFront(closest))
            {
                if (debugLog) Debug.Log("[Target] closest 조건 통과 → 타겟 확정");
                return closest;
            }
            else
            {
                if (debugLog) Debug.Log("[Target] closest 조건 실패 → fallback 탐색");
            }
        }
        else
        {
            if (debugLog) Debug.Log("[Target] closest = null");
        }

        // 2차: 사거리+시야각 만족하는 것 중 최단거리
        ZombieNavMesh best = null;
        float bestDist = float.MaxValue;

        foreach (var z in NPCManager.Instance.Zombies)
        {
            if (z == null || !z.gameObject.activeInHierarchy)
            {
                if (debugLog) Debug.Log("[Target] 후보 스킵 (null / inactive)");
                continue;
            }

            float dist = Vector3.Distance(transform.position, z.transform.position);
            if (dist > shootRange)
            {
                if (debugLog) Debug.Log($"[Target] 후보 {z.name} 실패: 사거리 초과 ({dist})");
                continue;
            }

            if (!IsInFront(z))
            {
                if (debugLog) Debug.Log($"[Target] 후보 {z.name} 실패: 시야각 바깥");
                continue;
            }

            if (dist < bestDist)
            {
                if (debugLog) Debug.Log($"[Target] 후보 {z.name} → best 갱신");
                bestDist = dist;
                best = z;
            }
        }

        if (best != null)
        {
            if (debugLog) Debug.Log("[Target] fallback 타겟 = " + best.name);
        }
        else
        {
            if (debugLog) Debug.Log("[Target] fallback 실패 → 타겟 없음");
        }

        return best;
    }

    // ================= Vision =================
    protected bool IsInFront(ZombieNavMesh target)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        if (debugLog)
            Debug.Log($"[Vision] angle={angle}, limit={viewAngle * 0.5f}");

        return angle < viewAngle * 0.5f;
    }

    // ================= Animation Event =================
   public void OnShootEvent()
{
    ZombieNavMesh target = currentTarget;

    // 1️⃣ 원래 타겟이 죽었으면 즉시 대체 타겟 탐색
    if (target == null || !target.gameObject.activeInHierarchy)
    {
        target = FindShootableTarget();
    }

    // 2️⃣ 그래도 없으면 공포 (진짜 실패)
    if (target == null)
    {
        isShooting = false;
        shootTimer = shootInterval;
        PlayAnim("StickMan_Idle");
        return;
    }

    // 3️⃣ 무조건 1발은 나감
    SpawnMuzzleFX();
    PerformShot(target);

    shootTimer = shootInterval;
    isShooting = false;
    PlayAnim("StickMan_Idle");
}

    void SpawnMuzzleFX()
    {
        if (muzzlePoint == null)
            return;

        GameObject fx = PoolManager.Instance.Spawn(
            muzzlePoolKey,
            muzzlePoint.position,
            muzzlePoint.rotation
        );

        // 필요하면 약간 회전 보정
        // fx.transform.Rotate(0f, 180f, 0f);
    }

  protected void PerformShot(ZombieNavMesh target)
{
    Debug.Log("[Shooter] PerformShot → " + target.name);

    // 🔥 피격 이펙트 (풀링)
    Vector3 hitPos = target.GetHitPoint();

    PoolManager.Instance.Spawn(
        hitFxPoolKey,
        hitPos,
        Quaternion.LookRotation(transform.forward)
    );

    // 데미지 적용
    target.TakeDamage(damage);
}


   #if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    // 사거리/시야각은 기존 유지
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, shootRange);

    Gizmos.color = Color.yellow;
    Vector3 leftDir = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
    Vector3 rightDir = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

    Gizmos.DrawLine(transform.position, transform.position + leftDir * shootRange);
    Gizmos.DrawLine(transform.position, transform.position + rightDir * shootRange);

    Gizmos.color = Color.green;
    Gizmos.DrawLine(transform.position, transform.position + transform.forward * shootRange);

    // 현재 타겟 라인
    if (currentTarget != null)
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position + Vector3.up * 1.2f, currentTarget.transform.position + Vector3.up * 1.2f);
        Gizmos.DrawSphere(currentTarget.transform.position + Vector3.up * 1.2f, 0.25f);
    }

    // 상태 라벨
    Handles.Label(
        transform.position + Vector3.up * 2.4f,
        $"[Shooter]\n" +
        $"current={((currentTarget != null) ? currentTarget.name : "null")}\n" +
        $"last={((lastTarget != null) ? lastTarget.name : "null")}\n" +
        $"isShooting={isShooting}\n" +
        $"shootTimer={shootTimer:F2}\n" +
        $"{debugReason}"
    );
}
#endif

    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Shooter", gameObject);
    }
    protected override void PlayAnim(string animName)
    {
        if (anim == null) return;

        switch (animName)
        {
            case "StickMan_Shoot":
                anim.ResetTrigger("Shoot");
                anim.SetTrigger("Shoot");
                break;

            case "StickMan_Idle":
                anim.ResetTrigger("Idle");
                anim.SetTrigger("Idle");
                break;
        }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (currentTarget == null) return;

        // 🔴 타겟 라인/구체는 그대로
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        Gizmos.DrawSphere(currentTarget.transform.position, 0.5f);

        // ✅ 텍스트를 "나 자신 머리 위"에 표시
        Handles.Label(
            transform.position + Vector3.up * 2.4f,
            $"State: {state}\n" +
           
            $"CmdLock: {isCommandLocked}\n" +
            $"Target: {currentTarget.name}"
        );
    }
#endif
}

