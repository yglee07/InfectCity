using UnityEngine;
using UnityEngine.AI;


    public class Door : MonoBehaviour
    {
        // =========================
        // State
        // =========================
        [Header("State")]
        public bool open;

        // =========================
        // Rotation
        // =========================
        [Header("Rotation")]
        public Transform doorPivot;          // 실제 회전하는 문
        public float smooth = 1.0f;
        public float doorOpenAngle = -90f;
        public float doorCloseAngle = 0f;

        // =========================
        // NavMesh
        // =========================
        [Header("NavMesh")]
        public NavMeshObstacle obstacle;
        public float openThreshold = 80f;    // 이 각도 이상이면 통과 가능

        // =========================
        // Citizen Detect
        // =========================
        [Header("Citizen Detect")]
        public float openDistance = 0.6f;    // 시민 목적지가 이 거리 이내면 열림
        public float checkInterval = 0.25f;  // 검사 주기

        // =========================
        // Auto Close
        // =========================
        [Header("Auto Close")]
        public float openHoldTime = 1.0f;    // 마지막 시민 이후 유지 시간

        // =========================
        // Internal
        // =========================
        bool navOpenApplied = false;
        float checkTimer = 0f;
        float openTimer = 0f;
        

        // =========================
        // Life Cycle
        // =========================
        void OnEnable()
        {
            NPCManager.Instance?.RegisterDoor(this);
        }

        void OnDisable()
        {
            NPCManager.Instance?.UnregisterDoor(this);
        }
        void Awake()
        {
            if (obstacle == null)
                obstacle = GetComponent<NavMeshObstacle>();

            if (obstacle != null)
                obstacle.enabled = true; // 시작은 닫힘

            if (doorPivot == null)
                Debug.LogError($"[Door] doorPivot not assigned on {name}");
        }

        void Update()
        {
            // 1️⃣ 시민 의도 검사 (저빈도)
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = checkInterval;
                CheckCitizenIntent();
            }

            // 2️⃣ 열림 유지 타이머
            if (open)
            {
                openTimer -= Time.deltaTime;
                if (openTimer <= 0f)
                {
                    open = false; // 닫기 시작
                }
            }

            // 3️⃣ 회전 애니메이션
            UpdateRotation();

            // 4️⃣ NavMeshObstacle 동기화
            UpdateNavMeshByAngle();
        }

        // =========================
        // Citizen → Door Open
        // =========================
        void CheckCitizenIntent()
{
    Debug.Log("[Door] CheckCitizenIntent called");

    var citizens = NPCManager.Instance.Citizens;
    Vector3 doorPos = transform.position;

    for (int i = 0; i < citizens.Count; i++)
    {
        var c = citizens[i];
        if (c == null || !c.gameObject.activeInHierarchy) continue;

        float dist = Vector3.Distance(c.transform.position, doorPos);

        if (dist <= openDistance)
        {
            Debug.Log($"[Door] Citizen nearby: {c.name}, dist={dist:F2}");

            open = true;
            openTimer = openHoldTime;
            return;
        }
    }
}


        // =========================
        // Rotation
        // =========================
        void UpdateRotation()
        {
            Debug.Log($"[Door] open={open} pivotY={doorPivot.localEulerAngles.y}");
            if (doorPivot == null) return;

            float targetAngle = open ? doorOpenAngle : doorCloseAngle;
            Quaternion targetRot = Quaternion.Euler(0f, targetAngle, 0f);

            doorPivot.localRotation = Quaternion.Slerp(
                doorPivot.localRotation,
                targetRot,
                Time.deltaTime * 5f * smooth
            );
        }

        // =========================
        // NavMesh Sync
        // =========================
        void UpdateNavMeshByAngle()
        {
            if (doorPivot == null || obstacle == null) return;

            float currentY = Mathf.Abs(NormalizeAngle(doorPivot.localEulerAngles.y));

            // 충분히 열렸을 때 → 통과 허용
            if (open && !navOpenApplied && currentY >= openThreshold)
            {
                navOpenApplied = true;
                obstacle.enabled = false;   // 🔓
            }

            // 거의 닫혔을 때 → 다시 차단
            if (!open && navOpenApplied && currentY <= 5f)
            {
                navOpenApplied = false;
                obstacle.enabled = true;    // 🔒
            }
        }

        float NormalizeAngle(float angle)
        {
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        // 🔥 시민이 문을 통과할 수 있는지 확인
        public bool IsPassable()
        {
            if (obstacle == null) return true; // obstacle이 없으면 항상 통과 가능
            
            // obstacle이 비활성화되어 있으면 통과 가능
            return !obstacle.enabled;
        }

        // 🔥 문이 열리는 중인지 확인 (open 상태이지만 아직 완전히 열리지 않음)
        public bool IsOpening()
        {
            return open && obstacle != null && obstacle.enabled;
        }
        public Vector3 GetApproachPoint()
        {
            // 문 앞 1.2m 지점 (NavMesh 위)
            Vector3 raw = transform.position + transform.forward * 1.2f;

            if (NavMesh.SamplePosition(raw, out var hit, 1.5f, NavMesh.AllAreas))
                return hit.position;

            // 실패하면 문 위치 반환 (fallback)
            return transform.position;
        }

     #if UNITY_EDITOR
void OnDrawGizmos()
{
    Vector3 doorPos = transform.position;

    bool hasCitizenNearby = false;

    if (NPCManager.Instance != null)
    {
        var citizens = NPCManager.Instance.Citizens;

        for (int i = 0; i < citizens.Count; i++)
        {
            var c = citizens[i];
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(c.transform.position, doorPos);
            if (dist <= openDistance)
            {
                hasCitizenNearby = true;
                break;
            }
        }
    }

    // 🔵 1단계: 거리 감지
    Gizmos.color = hasCitizenNearby ? Color.green : Color.cyan;
    Gizmos.DrawWireSphere(doorPos, openDistance);

    // 🔴 2단계: 실제 문 상태
    Gizmos.color = open ? Color.red : Color.gray;
    Gizmos.DrawSphere(doorPos, 0.15f);
}
#endif



public Vector3 GetPassThroughPoint(Vector3 fromPos, float dist = 2.5f)
{
    // 내가 문 앞에 어느 쪽에 있느냐로 방향 결정
    bool isOutside = Vector3.Dot(fromPos - transform.position, transform.forward) < 0f;

    Vector3 dir = isOutside ? transform.forward : -transform.forward;
    return transform.position + dir * dist;
}
        // =========================
        // Zombie Break
        // =========================
        public void BreakDoor()
        {
            open = true;
            navOpenApplied = true;
            openTimer = float.MaxValue;

            if (obstacle != null)
                obstacle.enabled = false;   // 영구 개방
        }
    }

