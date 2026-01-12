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

    // CameraController.cs
    float lastUserInputTime;
    [SerializeField] float idleAutoDelay = 5f;

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

        if (Game.Instance.dragInfector != null &&
            Game.Instance.dragInfector.IsDraggingBomb)
            return;

        if (Game.Instance.dragUnit != null &&
            Game.Instance.dragUnit.IsDraggingUnit)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
    HandleTouchInput();
#endif
        if (IsAutoCameraAllowed())
        {
            AutoAdjustToGreenZombies();
        }
        // ===============================
        // 🔥 자동 카메라 조절 (입력 없을 때)
        // ===============================
        bool noRecentInput =
       Time.time - lastUserInputTime > idleAutoDelay;

       

        // ===============================
        // 실제 적용
        // ===============================
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
            0.12f
        );
    }

    void AutoAdjustToGreenZombies()
    {
        var greens = NPCManager.Instance.GreenZombies;
        if (greens == null || greens.Count == 0)
            return;

        Camera cam = this.cam;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (var z in greens)
        {
            if (z == null) continue;

            Vector3 sp = cam.WorldToScreenPoint(z.transform.position);
            if (sp.z < 0) continue;

            min = Vector2.Min(min, sp);
            max = Vector2.Max(max, sp);
        }

        if (min.x == float.MaxValue)
            return;

        // ===============================
        // 🔥 1. 화면 여백 추가 (핵심)
        // ===============================
        float screenPadding = Mathf.Min(Screen.width, Screen.height) * 0.18f;
        min -= Vector2.one * screenPadding;
        max += Vector2.one * screenPadding;

        Vector2 centerScreen = (min + max) * 0.5f;
        Vector3 centerWorld =
            cam.ScreenToWorldPoint(
                new Vector3(centerScreen.x, centerScreen.y, cam.nearClipPlane)
            );

        float screenWidth = max.x - min.x;
        float screenHeight = max.y - min.y;

        float sizeByHeight =
            (screenHeight / Screen.height) * cam.orthographicSize;
        float sizeByWidth =
            (screenWidth / Screen.width) * cam.orthographicSize;

        float desiredZoom =
            Mathf.Max(sizeByHeight, sizeByWidth) * 1.35f;

        // ===============================
        // 🔥 2. 줌 급변 방지 (중요)
        // ===============================
        float maxZoomStepPerFrame = 0.25f;
        desiredZoom = Mathf.Clamp(
            desiredZoom,
            targetZoom - maxZoomStepPerFrame,
            targetZoom + maxZoomStepPerFrame
        );

        // ===============================
        // 🔥 3. 위치도 살짝 둔하게
        // ===============================
        Vector3 desiredPos = new Vector3(
            centerWorld.x,
            targetPos.y,
            centerWorld.z
        );

        targetPos = Vector3.Lerp(
            targetPos,
            desiredPos,
            0.12f
        );

        targetZoom = Mathf.Lerp(
            targetZoom,
            Mathf.Clamp(desiredZoom, minZoom, maxZoom),
            0.15f
        );
    }




    void HandleMouseInput()
    {
        // =====================
        // 마우스 다운
        // =====================
        if (Input.GetMouseButtonDown(0) ||
           Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.001f)
        {
            lastUserInputTime = Time.time;
        
        
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

            // 🔥 손 뗀 순간도 Idle 기준 리셋
            Debug.Log("lastUserInputTime reset on mouse up");
            lastUserInputTime = Time.time;
        }
    }  
    void HandleTouchInput()
{
        if (Input.touchCount > 0)
        {
            lastUserInputTime = Time.time;
        }

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
            Debug.Log("lastUserInputTime reset on mouse up");
            // 🔥 손 뗀 순간도 Idle 기준 리셋
            lastUserInputTime = Time.time;
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

    public void FocusOn(Transform center, Vector3 offset, float zoom)
    {
        StopAllCoroutines();

        Vector3 targetPos = center.position + offset;
        StartCoroutine(FocusLobbyRoutine(targetPos, zoom));
    }

    IEnumerator FocusLobbyRoutine(Vector3 targetPos, float zoom)
    {
        Vector3 startPos = transform.position;
        float startZoom = cam.orthographicSize;

        Quaternion fixedRot = Quaternion.identity; // (0,0,0)

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.2f;

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            // 🔒 로비 카메라는 항상 고정
            transform.rotation = fixedRot;
            cam.orthographicSize = Mathf.Lerp(startZoom, zoom, t);

            yield return null;
        }

        // 🔒 최종 강제 고정
        transform.rotation = fixedRot;
        cam.orthographicSize = zoom;
    }
    public void SnapTo(Transform center, Vector3 offset, float zoom)
    {
        StopAllCoroutines();

        transform.position = center.position + offset;
        transform.rotation = Quaternion.identity;

        cam.orthographicSize = zoom;

        targetPos = transform.position;
        targetZoom = zoom;
    }

    bool IsAutoCameraAllowed()
    {
        return Time.time - lastUserInputTime > idleAutoDelay;
    }
    void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float idleTime = Time.time - lastUserInputTime;
        bool autoAllowed = IsAutoCameraAllowed();

        GUILayout.BeginArea(
            new Rect(10, 10, 360, 150),
            GUI.skin.box
        );

        GUILayout.Label("<b>=== AUTO CAMERA DEBUG ===</b>",
            new GUIStyle(GUI.skin.label)
            {
                richText = true
            });

        GUILayout.Space(6);

        GUILayout.Label($"Idle Time : {idleTime:F2}s");
        GUILayout.Label($"Idle Auto Delay : {idleAutoDelay:F2}s");

        GUILayout.Space(6);

        GUILayout.Label(
            $"Auto Camera : <b>{(autoAllowed ? "ON" : "OFF")}</b>",
            new GUIStyle(GUI.skin.label)
            {
                richText = true,
                normal =
                {
                textColor = autoAllowed ? Color.green : Color.red
                }
            }
        );

        GUILayout.EndArea();
#endif
    }

    public void ResetIdleTimer()
    {
        lastUserInputTime = Time.time;
    }
}
