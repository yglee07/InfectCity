using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Move")]
    public float dragSpeed = 0.01f;

    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 5f;
    public float maxZoom = 25f;

    private Camera cam;

    [Header("Camera Bounds")]
    public float minX = -20f;
    public float maxX = 20f;
    public float minZ = -20f;
    public float maxZ = 20f;

    public Transform cameraPos;

    // 입력 상태
    private enum InputState { None, Drag, Zoom }
    private InputState inputState = InputState.None;

    // 드래그용
    private Vector3 lastDragWorldPos;

    // 줌용 (모바일)
    private float prevPinchDistance;

    void Awake()
    {
        cam = Camera.main;
        cam.orthographic = true; // Orthographic 강제
    }

    void LateUpdate()
    {
        if (Game.Instance == null || !Game.Instance.gameObject.activeInHierarchy)
            return;

        // 폭탄 드래그 중이면 카메라 멈춤
        if (Game.Instance.dragInfector != null &&
            Game.Instance.dragInfector.IsDraggingBomb)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();          // ★ PC 전용 입력 추가
#else
    HandleTouchInput();          // 모바일 입력 계속 사용
#endif

        ClampPosition();
    }

    void HandleMouseInput()
    {
        // =====================
        // 줌 (마우스 휠)
        // =====================
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            cam.orthographicSize -= scroll * zoomSpeed * 10f; // 속도 보정
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        // =====================
        // 드래그 (좌클 or 우클)
        // =====================
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            lastDragWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector3 newPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = lastDragWorldPos - newPos;
            diff.y = 0;

            // 줌 크기에 따라 이동량 증가
            float zoomFactor = cam.orthographicSize * dragMultiplier;

            transform.position += diff * zoomFactor;

            lastDragWorldPos = newPos;
        }
    }


    void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        // 아무 터치 없으면 상태 초기화
        if (touchCount == 0)
        {
            inputState = InputState.None;
            return;
        }

        // ------------------
        // 2손가락 → 줌 모드
        // ------------------
        if (touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 pos0 = t0.position;
            Vector2 pos1 = t1.position;

            float currDist = (pos0 - pos1).magnitude;

            // 핀치 시작
            if (inputState != InputState.Zoom)
            {
                inputState = InputState.Zoom;
                prevPinchDistance = currDist;
                return;
            }

            // 중간 지점 = 줌 앵커
            Vector2 mid = (pos0 + pos1) * 0.5f;
            Vector3 before = cam.ScreenToWorldPoint(mid);

            float diff = currDist - prevPinchDistance;
            prevPinchDistance = currDist;

            cam.orthographicSize -= diff * zoomSpeed * 0.03f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

            Vector3 after = cam.ScreenToWorldPoint(mid);

            // 줌으로 인해 밀린 만큼 보정
            Vector3 shift = before - after;
            shift.y = 0;   // ★ 필수

            transform.position += shift;


            return; // 줌 중에는 드래그 안 함
        }

        // ------------------
        // 1손가락 → 드래그 모드
        // ------------------
        if (touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                inputState = InputState.Drag;
                lastDragWorldPos = cam.ScreenToWorldPoint(t.position);
                dragVelocity = Vector3.zero; // 관성 초기화
            }
            else if (t.phase == TouchPhase.Moved && inputState == InputState.Drag)
            {
                Vector3 newPos = cam.ScreenToWorldPoint(t.position);
                Vector3 diff = lastDragWorldPos - newPos;

                diff.y = 0;

                // zoom 감도 반영
                float zoomFactor = cam.orthographicSize * dragMultiplier;

                // 목표 이동 위치 계산
                Vector3 target = transform.position + diff * zoomFactor;

                // 부드럽게 이동(SmoothDamp 사용)
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    target,
                    ref dragVelocity,
                    smoothTime
                );

                lastDragWorldPos = newPos;
            }
        }
    }
    private Vector3 dragVelocity = Vector3.zero;

    // 드래그 감도
    public float dragMultiplier = 0.1f;

    // 부드러움 정도
    public float smoothTime = 0.05f;


    // ===========================
    //  위치 클램프
    // ===========================
    void ClampPosition()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }

    public void SnapToOrigin()
    {
        if (cameraPos == null) return;

        transform.position = cameraPos.position;
        transform.rotation = cameraPos.rotation;
    }
}
