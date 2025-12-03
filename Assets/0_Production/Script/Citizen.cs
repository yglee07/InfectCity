using UnityEngine;

public class Citizen : MonoBehaviour
{
    public enum State
    {
        Wander,
        Flee
    }

    [Header("디버그용 현재 상태")]
    [SerializeField] private State currentState;

    [Header("상태별 이동 속도")]
    public float wanderSpeed = 2f;
    public float fleeSpeed = 4f;
    private float currentSpeed = 2f;

    [Header("회전 속도")]
    public float rotateSpeed = 5f;

    [Header("Wander 설정")]
    public float wanderRadius = 5f;
    public float changeTargetTime = 3f;
    private float wanderTimer;
    private Vector3 wanderTarget;

    [Header("시민 회피 설정")]
    public float avoidanceRadius = 1f;

    [Header("좀비 감지 설정 + Hysteresis")]
    public float fleeEnterRadius = 4f;  // 도망 시작
    public float fleeExitRadius = 6f;   // 도망 종료 (더 넓음 → 진동 방지)
    public float fleeDistance = 6f;
    public LayerMask zombieLayer;

    private Vector3 fleeTarget;

    void Start()
    {
        SetNewWanderTarget();
        ChangeState(State.Wander);
    }

    void Update()
    {
        bool detected = CheckZombieDetected();

        // 상태 전환 (Hysteresis 적용)
        if (detected && currentState != State.Flee)
        {
            SetNewFleeTarget();
            ChangeState(State.Flee);
        }
        else if (!detected && currentState != State.Wander)
        {
            SetNewWanderTarget();
            ChangeState(State.Wander);
        }

        // 상태별 로직
        if (currentState == State.Wander) UpdateWander();
        else UpdateFlee();
    }

    // ==========================
    // 상태 변화 처리
    // ==========================
    void ChangeState(State newState)
    {
        currentState = newState;

        if (newState == State.Wander)
            currentSpeed = wanderSpeed;
        else
            currentSpeed = fleeSpeed;
    }

    // ==========================
    // WANDER
    // ==========================
    void UpdateWander()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= changeTargetTime ||
            Vector3.Distance(transform.position, wanderTarget) < 0.7f)
        {
            SetNewWanderTarget();
            wanderTimer = 0f;
        }

        MoveTowards(wanderTarget, applyZombieAvoidance: false);
    }

    void SetNewWanderTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        wanderTarget = transform.position + new Vector3(rnd.x, 0, rnd.y);
    }

    // ==========================
    // FLEE (B 방식)
    // ==========================
    void UpdateFlee()
    {
        MoveTowards(fleeTarget, applyZombieAvoidance: true);

        if (Vector3.Distance(transform.position, fleeTarget) < 1.5f)
        {
            SetNewFleeTarget();
        }
    }

    void SetNewFleeTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, fleeEnterRadius, zombieLayer);
        if (hits.Length == 0)
        {
            ChangeState(State.Wander);
            return;
        }

        // 가장 가까운 좀비 찾기
        Transform nearest = hits[0].transform;
        float minDist = Vector3.Distance(transform.position, nearest.position);

        foreach (var h in hits)
        {
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = h.transform;
            }
        }

        // 좀비 반대 방향에 flee target 생성
        Vector3 away = (transform.position - nearest.position).normalized;

        Vector3 offset = new Vector3(
            Random.Range(-2f, 2f),
            0,
            Random.Range(-2f, 2f)
        );

        fleeTarget = transform.position + away * fleeDistance + offset;
    }

    // ==========================
    // 이동 (공통)
    // ==========================
    void MoveTowards(Vector3 targetPos, bool applyZombieAvoidance)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;

        Vector3 desiredDir = dir.normalized;

        // 시민 회피
        desiredDir += GetCitizenAvoidanceDir() * 0.5f;

        // 좀비 회피
        if (applyZombieAvoidance)
            desiredDir += GetZombieAvoidanceDir() * 1.0f;

        Vector3 finalDir = desiredDir.normalized;

        // 회전
        if (finalDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(finalDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // 이동
        transform.position += transform.forward * currentSpeed * Time.deltaTime;
    }

    // ==========================
    // 감지 (Hysteresis 적용)
    // ==========================
    bool CheckZombieDetected()
    {
        float radius = (currentState == State.Flee) ? fleeExitRadius : fleeEnterRadius;

        return Physics.OverlapSphere(transform.position, radius, zombieLayer).Length > 0;
    }

    // ==========================
    // 회피 기능
    // ==========================

    Vector3 GetCitizenAvoidanceDir()
    {
        Vector3 avoid = Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, avoidanceRadius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (!hit.CompareTag("Citizen")) continue;

            Vector3 away = transform.position - hit.transform.position;
            away.y = 0;
            avoid += away.normalized;
        }
        return avoid.normalized;
    }

    Vector3 GetZombieAvoidanceDir()
    {
        Vector3 avoid = Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, fleeEnterRadius, zombieLayer);

        foreach (var hit in hits)
        {
            Vector3 away = transform.position - hit.transform.position;
            away.y = 0;

            avoid += away.normalized;
        }

        return avoid.normalized;
    }

    // ==========================
    // Gizmos
    // ==========================
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 감지 반경
        Gizmos.color = currentState == State.Flee ? new Color(1, 0, 0, 0.25f) : new Color(0, 1, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position,
            currentState == State.Flee ? fleeExitRadius : fleeEnterRadius);

        // Wander target
        if (currentState == State.Wander)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(wanderTarget, 0.2f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }

        // Flee target
        if (currentState == State.Flee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(fleeTarget, 0.2f);
            Gizmos.DrawLine(transform.position, fleeTarget);
        }

        // Forward
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1.2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"{currentState}");
#endif
    }
}
