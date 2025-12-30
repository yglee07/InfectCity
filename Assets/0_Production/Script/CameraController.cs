using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
public class CameraController : MonoBehaviour
{
    [Header("Move")]
    public float dragSpeed = 0.01f;

    [Header("Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 5f;
    public float maxZoom = 25f;

    private Camera cam;

    //[Header("Camera Bounds")]
    //public float minX = -20f;
    //public float maxX = 20f;
    //public float minZ = -20f;
    //public float maxZ = 20f;
    public bool isCameraLocked = false;
    public Transform cameraPos;

    // 입력 상태
    private enum InputState { None, Drag, Zoom }
    private InputState inputState = InputState.None;

    // 드래그용
    private Vector3 lastDragWorldPos;

    // 줌용 (모바일)
    private float prevPinchDistance;
    [Header("Intro Move")]
    public Transform startPos;
    public Transform endPos;
    public float introDuration = 1.2f;

    private bool isIntroPlaying = false;
    [SerializeField]
    private float introStartZoom;
    [SerializeField]
    private float introEndZoom;

    // ===== Target 값 =====
    private Vector3 targetPos;
    private float targetZoom;
    private float zoomVelocity;
private bool blockCameraThisInput = false;
bool IsPointerOverUI()
{
#if UNITY_EDITOR || UNITY_STANDALONE
    return EventSystem.current != null &&
           EventSystem.current.IsPointerOverGameObject();
#else
    if (EventSystem.current == null) return false;
    if (Input.touchCount == 0) return false;

    return EventSystem.current.IsPointerOverGameObject(
        Input.GetTouch(0).fingerId
    );
#endif
}
    void Awake()
    {
        
        cam = Camera.main;
        cam.orthographic = true; // Orthographic 강제\
        targetPos = transform.position;        // ⭐ 추가
        targetZoom = cam.orthographicSize;     // ⭐ 추가
    }

    void LateUpdate()
    {
        if (isCameraLocked || isIntroPlaying)
            return;

        if (Game.Instance == null || !Game.Instance.gameObject.activeInHierarchy)
            return;

        // 폭탄 드래그 중이면 카메라 멈춤
        if (Game.Instance.dragInfector != null &&
            Game.Instance.dragInfector.IsDraggingBomb)
            return;

       #if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
        #else
        HandleTouchInput();
        #endif
        // ===== 실제 적용 =====
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref dragVelocity,
            smoothTime
        );

        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize,
            targetZoom,
            ref zoomVelocity,
            0.12f   // zoomSmoothTime (취향)
        );

    }

    void HandleMouseInput()
    {
         // =====================
    // 마우스 다운
    // =====================
    if (Input.GetMouseButtonDown(0))
    {
        // ⭐ UI에서 시작됐으면 카메라 입력 차단
        blockCameraThisInput = IsPointerOverUI();
        if (blockCameraThisInput)
            return;

        inputState = InputState.Drag;
        lastDragWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        dragVelocity = Vector3.zero;
    }

    if (blockCameraThisInput)
    {
        if (Input.GetMouseButtonUp(0))
            blockCameraThisInput = false;

        return;
    }
        // =====================
        // 줌 (마우스 휠)
        // =====================
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetZoom -= scroll * zoomSpeed * 10f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // =====================
        // 드래그 (마우스 좌클릭)
        // =====================
        if (Input.GetMouseButtonDown(0))
        {
            inputState = InputState.Drag;
            lastDragWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            dragVelocity = Vector3.zero;
        }

        if (Input.GetMouseButton(0) && inputState == InputState.Drag)
        {
            Vector3 newPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 diff = lastDragWorldPos - newPos;
            diff.y = 0;

            float zoomFactor = cam.orthographicSize * dragMultiplier;
            targetPos += diff * zoomFactor;

            lastDragWorldPos = newPos;
        }

        if (Input.GetMouseButtonUp(0))
        {
            inputState = InputState.None;
        }
    }   void HandleTouchInput()
{
    if (Input.touchCount == 0)
    {
        inputState = InputState.None;
        blockCameraThisInput = false;
        return;
    }

    // =====================
    // 2손가락 → 줌
    // =====================
    if (Input.touchCount == 2)
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        // ⭐ 핀치 시작 시점에서 UI 체크
        if (inputState != InputState.Zoom)
        {
            blockCameraThisInput =
                IsPointerOverUI() ||
                EventSystem.current.IsPointerOverGameObject(t0.fingerId) ||
                EventSystem.current.IsPointerOverGameObject(t1.fingerId);

            if (blockCameraThisInput)
                return;

            inputState = InputState.Zoom;
            prevPinchDistance = (t0.position - t1.position).magnitude;
            return;
        }

        if (blockCameraThisInput)
            return;

        // ---- 이하 기존 줌 로직 ----
        Vector2 p0 = t0.position;
        Vector2 p1 = t1.position;
        float dist = (p0 - p1).magnitude;

        Vector2 mid = (p0 + p1) * 0.5f;
        Vector3 before = cam.ScreenToWorldPoint(mid);

        float delta = dist - prevPinchDistance;
        prevPinchDistance = dist;

        targetZoom -= delta * zoomSpeed * 0.03f;
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);

        Vector3 after = cam.ScreenToWorldPoint(mid);
        Vector3 shift = before - after;
        shift.y = 0;

        targetPos += shift;
        return;
    }

    // =====================
    // 1손가락 → 드래그
    // =====================
    Touch t = Input.GetTouch(0);

    if (t.phase == TouchPhase.Began)
    {
        blockCameraThisInput = IsPointerOverUI();
        if (blockCameraThisInput)
            return;

        inputState = InputState.Drag;
        lastDragWorldPos = cam.ScreenToWorldPoint(t.position);
        dragVelocity = Vector3.zero;
    }

    if (blockCameraThisInput)
    {
        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            blockCameraThisInput = false;

        return;
    }

    if (t.phase == TouchPhase.Moved && inputState == InputState.Drag)
    {
        Vector3 newPos = cam.ScreenToWorldPoint(t.position);
        Vector3 diff = lastDragWorldPos - newPos;
        diff.y = 0;

        float zoomFactor = cam.orthographicSize * dragMultiplier;
        targetPos += diff * zoomFactor;

        lastDragWorldPos = newPos;
    }

    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
    {
        inputState = InputState.None;
        blockCameraThisInput = false;
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
    //void ClampPosition()
    //{
    //    Vector3 pos = transform.position;

    //    pos.x = Mathf.Clamp(pos.x, minX, maxX);
    //    pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

    //    transform.position = pos;
    //}

    public void SnapToOrigin()
    {
        if (cameraPos == null) return;

        transform.position = cameraPos.position;
        transform.rotation = cameraPos.rotation;
    }

    public void PlayIntro(
      Transform start,
      Transform end,
      float startZoom,
      float endZoom
  )
    {
        isCameraLocked = true;

        Debug.Log($"[Intro] PlayIntro params startZoom={startZoom}, endZoom={endZoom}, levelStart={start?.name}, levelEnd={end?.name}");
        if (start == null || end == null)
            return;

        startPos = start;
        endPos = end;

        introStartZoom = startZoom;
        introEndZoom = endZoom;

        StopAllCoroutines();
        StartCoroutine(IntroRoutine());


    }

    IEnumerator IntroRoutine()
    {
        isIntroPlaying = true;

        // 시작 상태 세팅
        transform.position = startPos.position;
        transform.rotation = startPos.rotation;
        cam.orthographicSize = introStartZoom;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / introDuration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(
                startPos.position,
                endPos.position,
                eased
            );

            transform.rotation = Quaternion.Slerp(
                startPos.rotation,
                endPos.rotation,
                eased
            );

            cam.orthographicSize = Mathf.Lerp(
                introStartZoom,
                introEndZoom,
                eased
            );

            yield return null;
        }

        // 보정
        transform.position = endPos.position;
        transform.rotation = endPos.rotation;
        cam.orthographicSize = introEndZoom;

        isIntroPlaying = false;
        isCameraLocked = false;
        targetPos = transform.position;
        targetZoom = cam.orthographicSize;
    }


}
