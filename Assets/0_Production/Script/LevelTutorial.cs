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
    [SerializeField] Transform newUnitTarget;     // 소개할 유닛
    [SerializeField] TMPro.TMP_Text unitNameText; // 유닛 이름

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
        SpecialZombie,
        NewUnit
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

        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        if (tutorialType == TutorialType.Infect)
            yield return InfectTutorial();
        else if (tutorialType == TutorialType.CameraZoom)
            yield return CameraZoomTutorial();
        else if (tutorialType == TutorialType.Speed)
            yield return SpeedTutorial();
        else if (tutorialType == TutorialType.Camera)
            yield return CameraTutorial();
        else if (tutorialType == TutorialType.NewUnit)
            yield return NewUnitTutorial();

        EndTutorial();
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

        Game.Instance.SetGameSpeed(1f);

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

    void EndTutorial()
    {
        Debug.Log("[Tutorial] END");

        if (Camera.main != null)
        {
            var camCtrl = Camera.main.GetComponent<CameraController>();
            if (camCtrl != null) camCtrl.isCameraLocked = false;
        }

        NPCManager.Instance.mutantChance = 0.1f;
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

        if (newUnitTarget == null || unitNameText == null)
            yield break;

        Camera cam = Camera.main;
        CameraController camCtrl = cam.GetComponent<CameraController>();

        // 1️⃣ 카메라 입력 잠금
        if (camCtrl != null)
            camCtrl.isCameraLocked = true;

        Time.timeScale = 0f;

        // 2️⃣ 원래 카메라 상태 저장
        Vector3 originPos = cam.transform.position;
        float originZoom = cam.orthographicSize;

        // 3️⃣ 유닛 기준으로 살짝 줌인
        Vector3 focus = newUnitTarget.position;
        cam.transform.position = new Vector3(
            focus.x,
            originPos.y,
            focus.z - 1.5f
        );
        cam.orthographicSize = originZoom * 0.65f;

        // 4️⃣ 유닛 이름 표시
        unitNameText.text = "MELEE FIGHTER"; // 여기만 유닛별로 바꾸면 됨
        unitNameText.gameObject.SetActive(true);

        // 5️⃣ 탭 대기
        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

        // 6️⃣ 복구
        unitNameText.gameObject.SetActive(false);

        cam.transform.position = originPos;
        cam.orthographicSize = originZoom;

        Time.timeScale = 1f;
        if (camCtrl != null)
            camCtrl.isCameraLocked = false;

        Debug.Log("[Tutorial] NEW UNIT COMPLETE");
    }

}
