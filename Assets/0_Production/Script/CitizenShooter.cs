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

    [Header("FX")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactFxPrefab;

    private ZombieNavMesh currentTarget;
    private ZombieNavMesh lastTarget;

    protected override void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (!agent.enabled)
            agent.enabled = true;

        agent.isStopped = true; // Wander 제거

        if (debugLog) Debug.Log("[Shooter] Start()");
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
        currentTarget = FindShootableTarget();

        if (currentTarget == null)
        {
            if (debugLog) Debug.Log("[Shooter] HandleCombat: 타겟 없음");
            return false;
        }

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
        if (debugLog) Debug.Log($"[Shooter] shootTimer = {shootTimer}");

        if (shootTimer <= 0f)
        {
            if (debugLog) Debug.Log("[Shooter] ▶ PlayAnim(\"Shoot\")");
            PlayAnim("Shoot");

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
        Debug.Log("[Shooter] OnShootEvent() 호출");

        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            Debug.Log("[Shooter] OnShootEvent 실패: 타겟 null / inactive");
            return;
        }

        PerformShot(currentTarget);
        shootTimer = shootInterval;
        PlayAnim("Idle");
    }

    protected void PerformShot(ZombieNavMesh target)
    {
        Debug.Log("[Shooter] PerformShot → " + target.name);

        if (muzzleFlashPrefab)
            Instantiate(muzzleFlashPrefab,
                transform.position + transform.forward * 0.4f,
                transform.rotation);

        if (impactFxPrefab)
            Instantiate(impactFxPrefab,
                target.transform.position + Vector3.up * 0.7f,
                Quaternion.identity);

        target.TakeDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);

        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.DrawLine(transform.position, transform.position + leftDir * shootRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * shootRange);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * shootRange);
    }
    protected override void DespawnSelf()
    {
        PoolManager.Instance.Despawn("Citizen_Shooter", gameObject);
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
            $"Idle: {isIdle}\n" +
            $"CmdLock: {isCommandLocked}\n" +
            $"Target: {currentTarget.name}"
        );
    }
#endif
}

