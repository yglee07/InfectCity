using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LevelTutorial : MonoBehaviour
{
    [Header("Tutorial Prefabs (UI)")]
    public GameObject fingerDragPrefab;   // 드래그 전용
    public GameObject fingerTouchPrefab;  // 터치(탭) 전용
    public GameObject infiniteCameraPrefab;  // 터치(탭) 전용
                                             // public GameObject touchCirclePrefab;

    [Header("New Unit Tutorial")]
    [SerializeField] Transform newUnitTarget;
    [SerializeField] string unitDisplayName;
    [SerializeField, TextArea] string unitDescription;

    GameObject fingerInstance;
    RectTransform fingerRT;

    Canvas canvas;
    RectTransform canvasRT;
    Camera uiCamera;


    public enum TutorialType
    {
        None,
        Speed,
        Camera,
        CameraZoom,   // 👈 추가
        Infect,
        DragUnit,
        SpecialZombie,
        NewUnit,
        NewUnitAndInfect   // 👈 추가
    }

    [Header("Tutorial")]
    public TutorialType tutorialType = TutorialType.None;

    void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            canvasRT = canvas.GetComponent<RectTransform>();
            uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;
        }
    }

    void Start()
    {
        if (tutorialType == TutorialType.None)
            return;
        int stage = SaveSystem.Data.stage;
        // 🔥 31 스테이지부터는 튜토리얼 자체를 안 함
        if (stage >= 31)
        {
            Destroy(this);
            return;
        }

        // 🔥 이미 이 스테이지 튜토리얼 봤으면 스킵
        if (SaveSystem.Data.IsTutorialCleared(stage))
        {
            Destroy(this);
            return;
        }
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        switch (tutorialType)
        {
            case TutorialType.Infect:
                yield return InfectTutorial();
                break;

            case TutorialType.CameraZoom:
                yield return CameraZoomTutorial();
                break;
            case TutorialType.DragUnit:
                yield return DragUnitTutorial();
                break;
            case TutorialType.Speed:
                yield return SpeedTutorial();
                break;

            case TutorialType.Camera:
                yield return CameraTutorial();
                break;

            case TutorialType.NewUnit:
                yield return NewUnitTutorial();
                break;

            case TutorialType.NewUnitAndInfect:
                yield return NewUnitAndInfectTutorial();
                break;
        }
        if (tutorialCanceled)
            yield break;

        EndTutorial();
    }

    IEnumerator NewUnitAndInfectTutorial()
    {
        Debug.Log("[Tutorial] NEW UNIT + INFECT");

        CameraController camCtrl = Camera.main.GetComponent<CameraController>();

        // 0️⃣ 인트로 끝날 때까지 대기
        yield return new WaitUntil(() =>
            camCtrl == null || camCtrl.isCameraLocked == false
        );

        // 1️⃣ 유닛 소개
        yield return NewUnitTutorial_Internal();

        // 살짝 텀 (연출 안정용)
        yield return new WaitForSeconds(0.2f);

        // 2️⃣ 감염 튜토리얼
        yield return InfectTutorial_Internal();

        Debug.Log("[Tutorial] NEW UNIT + INFECT COMPLETE");
    }
    IEnumerator NewUnitTutorial_Internal()
    {
        Debug.Log("[Tutorial] NEW UNIT (Internal)");

        if (newUnitTarget == null || Game.Instance == null)
            yield break;

        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(false);

        Camera cam = Camera.main;
        CameraController camCtrl = cam.GetComponent<CameraController>();

        // 인트로 끝 대기
        yield return new WaitUntil(() => camCtrl == null || !camCtrl.isCameraLocked);

        if (camCtrl != null)
            camCtrl.isCameraLocked = true;

        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(false);

        Game.Instance.EnterTutorial();
       
        Vector3 originPos = cam.transform.position;
        float originZoom = cam.orthographicSize;

        Renderer r = newUnitTarget.GetComponentInChildren<Renderer>();
        if (r == null) yield break;

        Vector3 focusPos = GetUnitFocusPosition(r, cam);
        float focusZoom = CalculateZoomToFitUnit(r, cam, 0.5f);

        yield return SmoothFocusCamera(
            cam,
            originPos,
            originZoom,
            focusPos,
            focusZoom,
            0.45f
        );
        
        Game.Instance.uiTutorial.ShowUnitIntro(
            unitDisplayName,
            unitDescription
        );

        // 🔥 무조건 3초 감상
        yield return new WaitForSecondsRealtime(3f);

        Game.Instance.uiTutorial.HideUnitIntro();

        yield return SmoothFocusCamera(
            cam,
            cam.transform.position,
            cam.orthographicSize,
            originPos,
            originZoom,
            0.35f
        );
        OnNewUnitTutorialEnd();
        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(true);
        if (camCtrl != null)
            camCtrl.isCameraLocked = false;
    }
    IEnumerator InfectTutorial_Internal()
    {
        Debug.Log("[Tutorial] INFECT (Internal)");

        if (fingerDragPrefab == null ||
            Game.Instance == null ||
            Game.Instance.uiGame == null ||
            Game.Instance.uiGame.bombButton == null)
            yield break;

        CameraController camCtrl = Camera.main.GetComponent<CameraController>();
        if (camCtrl != null)
            camCtrl.isCameraLocked = true;

        if (Game.Instance.dragUnit != null)
            Game.Instance.dragUnit.enabled = false;

        CitizenBase target = FindAnyCitizen();
        if (target == null)
            yield break;

        RectTransform buttonRT =
            Game.Instance.uiGame.bombButton.GetComponent<RectTransform>();

        Canvas c = buttonRT.GetComponentInParent<Canvas>();
        RectTransform cRT = c.GetComponent<RectTransform>();
        Camera uiCam = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;

        SpawnFingerDrag(buttonRT);

        while (NPCManager.Instance.GreenZombies.Count == 0)
        {
            Vector2 from = WorldToCanvas(
                buttonRT.TransformPoint(buttonRT.rect.center),
                cRT,
                uiCam
            );

            Vector2 to = WorldToCanvasFromMainCamera(
                GetCitizenVisualCenter(target),
                cRT
            );

            yield return PlayFingerDragAnchored(from, to);
            yield return new WaitForSeconds(0.3f);
        }

        ClearFinger();

        if (camCtrl != null)
            camCtrl.isCameraLocked = false;
    }

    // =====================================================
    // INFECT
    // =====================================================
    IEnumerator InfectTutorial()
    {
        Debug.Log("[Tutorial] INFECT");

        if (fingerDragPrefab == null ||
            Game.Instance == null ||
            Game.Instance.uiGame == null ||
            Game.Instance.uiGame.bombButton == null)
        {
            yield break;
        }

        CameraController camCtrl = Camera.main.GetComponent<CameraController>();
        if (camCtrl != null)
            camCtrl.isCameraLocked = true;

        if (Game.Instance.dragUnit != null)
            Game.Instance.dragUnit.enabled = false;

        CitizenBase targetCitizen = FindAnyCitizen();
        if (targetCitizen == null)
            yield break;

        RectTransform infectButtonRT =
            Game.Instance.uiGame.bombButton.GetComponent<RectTransform>();

        Canvas canvas = infectButtonRT.GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        SpawnFingerDrag(infectButtonRT);

        while (NPCManager.Instance.GreenZombies.Count == 0)
        {
            // 🔥 버튼 중심
            Vector2 from = WorldToCanvas(
                infectButtonRT.TransformPoint(infectButtonRT.rect.center),
                canvasRT,
                uiCam
            );

            // 🔥 시민 위치
            Vector2 to = WorldToCanvasFromMainCamera(
     GetCitizenVisualCenter(targetCitizen),
     canvasRT
 );

            yield return PlayFingerDragAnchored(from, to);
            yield return new WaitForSeconds(0.3f);
        }

        ClearFinger();
        Debug.Log("[Tutorial] INFECT COMPLETE");
    }


    // =====================================================
    // SPEED
    // =====================================================
    IEnumerator SpeedTutorial()
    {
        Debug.Log("[Tutorial] SPEED");

        if (fingerTouchPrefab == null ||
            //touchCirclePrefab == null ||
            canvasRT == null ||
            Game.Instance == null ||
            Game.Instance.uiGame == null ||
            Game.Instance.uiGame.speedToggleButton == null)
        {
            Debug.LogWarning("[Tutorial] Speed tutorial setup missing");
            yield break;
        }

        Button speedBtn = Game.Instance.uiGame.speedToggleButton;
        RectTransform speedRT = speedBtn.GetComponent<RectTransform>();

        // 원 파티클(UI) 스폰: 버튼과 동일 기준으로
        //GameObject circle = Instantiate(touchCirclePrefab, canvas.transform);
        //RectTransform circleRT = circle.GetComponent<RectTransform>();
        //CopyRectLike(speedRT, circleRT);
        //circleRT.anchoredPosition = speedRT.anchoredPosition;
        //circleRT.localScale = Vector3.one;

        // 손가락도 버튼 위에 고정 스폰
        SpawnFingerTouch(speedRT);

       

        yield return new WaitUntil(() => Game.Instance.GameSpeed > 1f);

        //Destroy(circle);
        ClearFinger();

        Debug.Log("[Tutorial] SPEED COMPLETE");
    }

    // =====================================================
    // UI Helpers
    // =====================================================
    //void SpawnFingerLike(RectTransform targetButtonRT)
    //{
    //    ClearFinger();

    //    fingerInstance = Instantiate(fingerPrefab, canvas.transform);
    //    fingerRT = fingerInstance.GetComponent<RectTransform>();

    //    CopyRectLike(targetButtonRT, fingerRT);
    //    fingerRT.pivot = new Vector2(0.5f, 1f);   // 손끝 기준
    //    fingerRT.anchoredPosition = targetButtonRT.anchoredPosition;
    //    fingerRT.localScale = Vector3.one;
    //    fingerRT.gameObject.SetActive(true);
    //}

    IEnumerator PlayFingerDragAnchored(Vector2 from, Vector2 to)
    {
        if (fingerRT == null)
            yield break;

        fingerRT.gameObject.SetActive(true);
        fingerRT.anchoredPosition = from; // 🔥 시작점 고정

        float duration = 0.6f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            fingerRT.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }
    }

    Vector2 WorldToCanvasAnchored(Vector3 worldPos)
    {
        Vector3 screen = Camera.main.WorldToScreenPoint(worldPos);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screen,
            uiCamera,
            out localPoint
        );

        // canvasRT의 localPoint는 Pivot 기준 로컬좌표
        // anchoredPosition은 부모 기준인데, 여기서는 canvas 하위에 직접 붙이므로 localPoint 그대로 써도 됨
        return localPoint;
    }

    void CopyRectLike(RectTransform src, RectTransform dst)
    {
        dst.anchorMin = src.anchorMin;
        dst.anchorMax = src.anchorMax;
        //dst.pivot = src.pivot;
        //dst.sizeDelta = src.sizeDelta;
    }

    void ClearFinger()
    {
        if (fingerInstance != null)
            Destroy(fingerInstance);
        fingerInstance = null;
        fingerRT = null;
    }
    
    CitizenBase FindAnyCitizen()
    {
        if (NPCManager.Instance.Citizens.Count == 0)
            return null;
        return NPCManager.Instance.Citizens[0];
    }

    public void EndTutorial()
    {
        if (tutorialCanceled || tutorialFinished)
            return;

        tutorialFinished = true;

        Debug.Log("[Tutorial] END (CLEARED)");

        int stage = SaveSystem.Data.stage;
        SaveSystem.Data.MarkTutorialCleared(stage);
        SaveSystem.Save();

        if (Camera.main != null)
        {
            var camCtrl = Camera.main.GetComponent<CameraController>();
            if (camCtrl != null) camCtrl.isCameraLocked = false;
        }

        Destroy(this);
    }


    void SpawnFingerDrag(RectTransform targetRT)
    {
        ClearFinger();

        Canvas targetCanvas = targetRT.GetComponentInParent<Canvas>();

        fingerInstance = Instantiate(
            fingerDragPrefab,
            targetCanvas.transform
        );

        fingerRT = fingerInstance.GetComponent<RectTransform>();

        fingerRT.localScale = Vector3.one;
        fingerRT.gameObject.SetActive(false); // 위치 잡기 전까지 숨김
    }


    void SpawnFingerTouch(RectTransform targetRT)
    {
        ClearFinger();

        // 버튼이 속한 Canvas
        Canvas targetCanvas = targetRT.GetComponentInParent<Canvas>();
        RectTransform canvasRT = targetCanvas.GetComponent<RectTransform>();

        fingerInstance = Instantiate(fingerTouchPrefab, targetCanvas.transform);
        fingerRT = fingerInstance.GetComponent<RectTransform>();

        // ❌ anchor / pivot / size 절대 안 건드림
        fingerRT.localScale = Vector3.one;

        // 🔥 버튼 "월드 중앙"을 Canvas 좌표로 변환
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            RectTransformUtility.WorldToScreenPoint(
                targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : targetCanvas.worldCamera,
                targetRT.TransformPoint(targetRT.rect.center)
            ),
            targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : targetCanvas.worldCamera,
            out localPos
        );

        fingerRT.anchoredPosition = localPos;
        fingerRT.gameObject.SetActive(true);
    }



    IEnumerator CameraTutorial()
    {
        Debug.Log("[Tutorial] CAMERA");

        CameraController cam = Camera.main.GetComponent<CameraController>();

        // 1️⃣ 인트로 종료 대기
        yield return new WaitUntil(() => cam.isCameraLocked == false);

        // 2️⃣ infinite 힌트 표시
        GameObject hint = Instantiate(infiniteCameraPrefab, canvas.transform);

        RectTransform rt = hint.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        // 3️⃣ 기준값 재설정 (중요)
        Vector3 startPos = Camera.main.transform.position;
        float startZoom = Camera.main.orthographicSize;

        // 4️⃣ 실제 플레이어 조작 대기
        yield return new WaitUntil(() =>
            Vector3.Distance(Camera.main.transform.position, startPos) > 0.25f ||
            Mathf.Abs(Camera.main.orthographicSize - startZoom) > 0.25f
        );

        Destroy(hint);
        Debug.Log("[Tutorial] CAMERA COMPLETE");
    }
    IEnumerator CameraZoomTutorial()
    {
        Debug.Log("[Tutorial] CAMERA ZOOM");

        CameraController cam = Camera.main.GetComponent<CameraController>();

        // 🔒 인트로 끝날 때까지 대기
        yield return new WaitUntil(() => cam.isCameraLocked == false);

        // =========================
        // STEP 1 : 슬라이더 이동
        // =========================
        yield return ZoomSliderStep();

        // =========================
        // STEP 2 : Reset 버튼 클릭
        // =========================
        yield return ZoomResetButtonStep();

        Debug.Log("[Tutorial] CAMERA ZOOM COMPLETE");
    }
    IEnumerator ZoomSliderStep()
    {
        Slider zoomSlider = Game.Instance.uiGame.zoomSlider;
        RectTransform sliderRT = zoomSlider.GetComponent<RectTransform>();

        float startValue = zoomSlider.value;

        // 손가락 스폰 (위치는 아래에서 잡음)
        SpawnFingerDrag(sliderRT);

        Canvas targetCanvas = sliderRT.GetComponentInParent<Canvas>();
        RectTransform canvasRT = targetCanvas.GetComponent<RectTransform>();

        bool isVertical =
            zoomSlider.direction == Slider.Direction.BottomToTop ||
            zoomSlider.direction == Slider.Direction.TopToBottom;

        // 🔥 슬라이더 월드 기준점
        Vector3 sliderWorldCenter = GetSliderWorldCenter(sliderRT);

        Vector2 center = UIWorldToCanvas(
            sliderWorldCenter,
            canvasRT,
            targetCanvas
        );
        Vector2 from, to;

        if (isVertical)
        {
            from = center + new Vector2(0f, -80f);
            to = center + new Vector2(0f, 80f);
        }
        else
        {
            from = center + new Vector2(-80f, 0f);
            to = center + new Vector2(80f, 0f);
        }

        while (Mathf.Approximately(zoomSlider.value, startValue))
        {
            yield return PlayFingerDragAnchored(from, to);
            yield return new WaitForSeconds(0.4f);
        }

        ClearFinger();
        yield return null;
    }

    bool zoomResetClicked = false;
    IEnumerator ZoomResetButtonStep()
    {
        Button resetBtn = Game.Instance.uiGame.zoomResetButton;
        RectTransform resetRT = resetBtn.GetComponent<RectTransform>();

        bool clicked = false;

        void OnResetClicked()
        {
            clicked = true;
        }

        // 🔥 임시 리스너 등록
        resetBtn.onClick.AddListener(OnResetClicked);

        SpawnFingerTouch(resetRT);

        yield return new WaitUntil(() => clicked);

        // 🔥 반드시 제거
        resetBtn.onClick.RemoveListener(OnResetClicked);

        ClearFinger();
    }
    Vector2 GetAnchoredPosFromTargetTopLeft(RectTransform targetRT)
    {
        Vector3[] corners = new Vector3[4];
        targetRT.GetWorldCorners(corners);

        // corners[1] = 좌상단
        Vector3 worldTopLeft = corners[1];

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            worldTopLeft
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screenPos,
            uiCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    Vector2 WorldToCanvas(Vector3 worldPos, RectTransform canvasRT, Camera cam)
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screenPos,
            cam,
            out Vector2 localPos
        );

        return localPos;
    }
    Vector2 WorldToCanvasFromMainCamera(
    Vector3 worldPos,
    RectTransform canvasRT
)
    {
        // 1️⃣ 3D 월드를 메인 카메라로 스크린 좌표 변환
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        // 2️⃣ 스크린 → 캔버스 로컬 좌표
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screenPos,
            null, // ScreenSpaceOverlay 기준
            out Vector2 localPos
        );

        return localPos;
    }
    Vector3 GetCitizenVisualCenter(CitizenBase citizen)
    {
        Renderer r = citizen.GetComponentInChildren<Renderer>();
        if (r != null)
            return r.bounds.center;

        return citizen.transform.position;
    }
    Vector3 GetSliderWorldCenter(RectTransform sliderRT)
    {
        Vector3[] corners = new Vector3[4];
        sliderRT.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }
    Vector2 UIWorldToCanvas(
    Vector3 worldPos,
    RectTransform canvasRT,
    Canvas canvas
)
    {
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screenPos,
            cam,
            out Vector2 localPos
        );

        return localPos;
    }
    IEnumerator NewUnitTutorial()
    {
        Debug.Log("[Tutorial] NEW UNIT");

        if (newUnitTarget == null || Game.Instance == null || Game.Instance.uiGame == null)
            yield break;
        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(false);

        Camera cam = Camera.main;
        CameraController camCtrl = cam.GetComponent<CameraController>();

        // 0️⃣ 인트로 종료 대기
        yield return new WaitUntil(() => camCtrl == null || camCtrl.isCameraLocked == false);

        // 1️⃣ 카메라 잠금
        if (camCtrl != null)
            camCtrl.isCameraLocked = true;
        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(false);
        Game.Instance.EnterTutorial();

        // 2️⃣ 원래 카메라 상태 저장
        Vector3 originPos = cam.transform.position;
        float originZoom = cam.orthographicSize;

        // 3️⃣ 유닛 Renderer 확보
        Renderer unitRenderer = newUnitTarget.GetComponentInChildren<Renderer>();
        if (unitRenderer == null)
            yield break;

        // 🔒 중심 고정
        Vector3 targetPos = GetUnitFocusPosition(unitRenderer, cam);
        float targetZoom = CalculateZoomToFitUnit(unitRenderer, cam, 0.5f);

        // 🔥 부드러운 확대
        yield return StartCoroutine(
            SmoothFocusCamera(
                cam,
                originPos,
                originZoom,
                targetPos,
                targetZoom,
                0.45f
            )
        );


        // 6️⃣ 유닛 이름 표시 (null-safe)
        Game.Instance.uiTutorial.ShowUnitIntro(
    unitDisplayName,
    unitDescription
);

        // 7️⃣ 입력 대기
        yield return new WaitForSecondsRealtime(3f);

        // 8️⃣ UI 숨김
        Game.Instance.uiTutorial.HideUnitIntro();

        // 9️⃣ 🔥 원래 상태로 부드럽게 복귀
        yield return StartCoroutine(
            SmoothFocusCamera(
                cam,
                cam.transform.position,
                cam.orthographicSize,
                originPos,
                originZoom,
                0.35f
            )
        );

        OnNewUnitTutorialEnd();
        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(true);

        if (camCtrl != null)
            camCtrl.isCameraLocked = false;

        Debug.Log("[Tutorial] NEW UNIT COMPLETE");
    }
    Vector3 GetUnitFocusPosition(Renderer r, Camera cam)
    {
        // 카메라가 보는 평면 (카메라 forward 기준)
        Plane cameraPlane = new Plane(
            cam.transform.forward,
            cam.transform.position
        );

        // 유닛 중심에서 카메라 방향으로 Ray 생성
        Ray ray = new Ray(
            r.bounds.center,
            -cam.transform.forward
        );

        // Ray가 카메라 평면과 만나는 지점
        if (cameraPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        // fallback (이론상 여기 안 옴)
        return cam.transform.position;
    }



    float CalculateZoomToFitUnit(Renderer r, Camera cam, float padding = 0.9f)
    {
        Bounds b = r.bounds;

        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;

        // 카메라 기준 가로/세로 크기
        float sizeRight =
            Mathf.Abs(Vector3.Dot(b.extents, right)) * 2f;
        float sizeUp =
            Mathf.Abs(Vector3.Dot(b.extents, up)) * 2f;

        float screenRatio = (float)Screen.width / Screen.height;

        float size;

        if (sizeRight / sizeUp > screenRatio)
            size = sizeRight / screenRatio;
        else
            size = sizeUp;

        return (size * 0.5f) / padding;
    }


    IEnumerator SmoothFocusCamera(
    Camera cam,
    Vector3 fromPos,
    float fromZoom,
    Vector3 toPos,
    float toZoom,
    float duration = 0.4f
)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration; // TimeScale=0 대응
            float eased = Mathf.SmoothStep(0f, 1f, t);

            cam.transform.position = Vector3.Lerp(fromPos, toPos, eased);
            cam.orthographicSize = Mathf.Lerp(fromZoom, toZoom, eased);

            yield return null;
        }

        cam.transform.position = toPos;
        cam.orthographicSize = toZoom;
    }
    bool tutorialCanceled = false;
    bool tutorialFinished = false;
    public void CancelTutorial()
    {
        if (tutorialCanceled) return;

        tutorialCanceled = true;
        StopAllCoroutines();
        ClearFinger();

        // 🔓 카메라 / UI 복구
        if (Camera.main != null)
        {
            var cam = Camera.main.GetComponent<CameraController>();
            if (cam != null) cam.isCameraLocked = false;
        }

        Destroy(this);
    }
    IEnumerator DragUnitTutorial()
    {
        Debug.Log("[Tutorial] DRAG UNIT");

        if (fingerDragPrefab == null ||
            Game.Instance == null ||
            Game.Instance.uiGame == null ||
            Game.Instance.uiGame.unitButton == null ||
            Game.Instance.dragUnit == null)
        {
            yield break;
        }
        Game.Instance.dragInfector.Deactivate();
        Game.Instance.dragInfector.currentCharges = 0;
     
        Game.Instance.uiGame.UpdateCharges(Game.Instance.dragInfector.currentCharges, Game.Instance.dragInfector.maxCharges);
        Game.Instance.uiGame.RefreshActionButtons(
    Game.Instance.dragInfector.currentCharges,
    Game.Instance.dragUnit.currentCharges
);
        CameraController camCtrl = Camera.main.GetComponent<CameraController>();
        if (camCtrl != null)
            camCtrl.isCameraLocked = true;

        // DragInfect 막고 DragUnit만 허용
        if (Game.Instance.dragInfector != null)
            Game.Instance.dragInfector.enabled = false;

        Game.Instance.dragUnit.enabled = true;

        CitizenBase targetCitizen = FindAnyCitizen();
        if (targetCitizen == null)
            yield break;

        RectTransform unitButtonRT =
            Game.Instance.uiGame.unitButton.GetComponent<RectTransform>();

        Canvas canvas = unitButtonRT.GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        SpawnFingerDrag(unitButtonRT);

        // 🔥 DragUnit 횟수가 0 될 때까지
        while (Game.Instance.dragUnit.currentCharges > 0)
        {
            Vector2 from = WorldToCanvas(
                unitButtonRT.TransformPoint(unitButtonRT.rect.center),
                canvasRT,
                uiCam
            );

            Vector2 to = WorldToCanvasFromMainCamera(
                GetCitizenVisualCenter(targetCitizen),
                canvasRT
            );

            yield return PlayFingerDragAnchored(from, to);
            yield return new WaitForSeconds(0.3f);
        }

        ClearFinger();

        if (camCtrl != null)
            camCtrl.isCameraLocked = false;

        Debug.Log("[Tutorial] DRAG UNIT COMPLETE");
        //Game.Instance.dragInfector.Activate();
    }
    void OnNewUnitTutorialStart()
    {
        Game.Instance.EnterTutorial();

        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(false);
    }

    void OnNewUnitTutorialEnd()
    {
        if (Game.Instance.uiGame != null)
            Game.Instance.uiGame.gameObject.SetActive(true);

        Game.Instance.ExitTutorial();
    }

}
