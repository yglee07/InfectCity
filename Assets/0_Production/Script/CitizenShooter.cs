using UnityEngine;

public class CitizenShooter : CitizenNavMesh
{
    public enum ShooterMode { Stationary, Mobile }
    public ShooterMode mode = ShooterMode.Mobile; // Inspector에서 선택

    [Header("Shooter Settings")]
    public float shootRange = 5f;
    public float shootInterval = 2f;
    public int damage = 1;
    protected float shootTimer = 0f;

    [Header("Vision")]
    public float viewAngle = 90f;

    public GameObject muzzleFlashPrefab;
    public GameObject impactFxPrefab;

    protected ZombieNavMesh currentTarget;

    protected override void Update()
    {
        // 전투 시도
        if (HandleCombat())
            return;

        // 전투 중이 아니면 이동 모드에 따라 행동
        if (mode == ShooterMode.Mobile)
            UpdateWanderOnly();      // 기존 시민처럼 돌아다님
        else
            StayIdle();              // 정지형
    }

    void StayIdle()
    {
        agent.isStopped = true;
        PlayAnim("Idle");
    }

    // ============================
    // Combat Logic
    // ============================
    protected bool HandleCombat()
    {
        currentTarget = NPCManager.Instance.FindClosestZombie(transform.position);
        if (currentTarget == null)
            return false;

        // 시야각 체크
        if (!IsInFront(currentTarget))
            return false;

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);

        // 사거리 밖
        if (dist > shootRange)
        {
            if (mode == ShooterMode.Mobile)
            {
                // 이동형만 접근한다
                agent.isStopped = false;
                agent.SetDestination(currentTarget.transform.position);
                PlayAnim("Run");
                return true;
            }

            // 정지형은 사거리 밖이면 대기
            StayIdle();
            return true;
        }

        // 사거리 안 — 사격
        agent.isStopped = true;

        Vector3 look = currentTarget.transform.position;
        look.y = transform.position.y;
        transform.LookAt(look);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            Shoot(currentTarget);
            shootTimer = shootInterval;
        }

        PlayAnim("Idle");
        return true;
    }

    protected bool IsInFront(ZombieNavMesh target)
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);

        return angle < viewAngle * 0.5f;
    }

    protected void Shoot(ZombieNavMesh target)
    {
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

    // ====================================
    // Wandering logic copied from parent
    // ====================================
    protected void UpdateWanderOnly()
    {
        if (isIdle)
        {
            agent.SetDestination(transform.position);

            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
                isIdle = false;

            return;
        }

        timer += Time.deltaTime;

        if (timer >= changeWanderInterval)
        {
            timer = 0f;
            isIdle = true;
            idleTimer = Random.Range(idleMin, idleMax);
            return;
        }

        agent.SetDestination(wanderTarget);
    }
}
