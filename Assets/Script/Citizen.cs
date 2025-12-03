using UnityEngine;

public class Citizen : MonoBehaviour
{
    [Header("이동 속도 설정")]
    [Tooltip("시민이 걷는 속도입니다.")]
    public float speed = 2f;

    [Header("회전 속도 설정")]
    [Tooltip("방향을 전환할 때 회전하는 속도입니다. 값이 높을수록 빨리 몸을 돌립니다.")]
    public float rotateSpeed = 5f;

    [Header("랜덤 이동 범위(Wander)")]
    [Tooltip("시민이 이동할 때 목표 지점을 생성하는 랜덤 반경입니다.")]
    public float wanderRadius = 5f;

    [Tooltip("새 목적지를 선택하기까지 걸리는 시간입니다.")]
    public float changeTargetTime = 3f;

    [Header("회피(Avoidance) 설정")]
    [Tooltip("주변 시민과의 충돌을 피하기 위해 감지하는 거리입니다.")]
    public float avoidanceRadius = 1f;


    private Vector3 targetPos;
    private float timer;

    void Start()
    {
        SetNewTarget();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 목표 지점 도달 or 시간 지나면 새 타겟
        if (timer >= changeTargetTime || Vector3.Distance(transform.position, targetPos) < 0.7f)
        {
            SetNewTarget();
            timer = 0f;
        }

        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;

        // 목표 방향
        Vector3 desiredDir = dir.normalized;

        // 회피 적용
        desiredDir += GetAvoidanceDir() * 0.5f;

        // 최종 방향
        Vector3 finalDir = desiredDir.normalized;

        // ? 회전: 항상 앞으로 보게 하기
        if (finalDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(finalDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        // ? 이동: transform.forward 기준으로만 이동 = 절대 뒤로 안 걷음
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void SetNewTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * wanderRadius;
        targetPos = new Vector3(transform.position.x + rnd.x, transform.position.y, transform.position.z + rnd.y);
    }

    Vector3 GetAvoidanceDir()
    {
        Vector3 avoid = Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, avoidanceRadius);
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            Vector3 away = transform.position - hit.transform.position;
            away.y = 0;
            avoid += away.normalized;
        }

        return avoid.normalized;
    }
}
