using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LevelTutorial : MonoBehaviour
{
    [Header("Tutorial Prefabs (UI)")]
    public GameObject fingerPrefab;
    public GameObject touchCirclePrefab;

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
        else if (tutorialType == TutorialType.Speed)
            yield return SpeedTutorial();

        EndTutorial();
    }

    // =====================================================
    // INFECT
    // =====================================================
    IEnumerator InfectTutorial()
    {
        Debug.Log("[Tutorial] INFECT");

        if (fingerPrefab == null ||
            canvasRT == null ||
            Game.Instance == null ||
            Game.Instance.uiGame == null ||
            Game.Instance.uiGame.bombButton == null)
        {
            Debug.LogWarning("[Tutorial] Infect tutorial setup missing");
            yield break;
        }

        // 감염만 허용
        Camera.main.GetComponent<CameraController>().isCameraLocked = true;
        if (Game.Instance.dragUnit != null)
            Game.Instance.dragUnit.enabled = false;

        CitizenBase targetCitizen = FindAnyCitizen();
        if (targetCitizen == null)
        {
            Debug.LogWarning("[Tutorial] No citizen found");
            yield break;
        }

        RectTransform infectButtonRT =
            Game.Instance.uiGame.bombButton.GetComponent<RectTransform>();

        SpawnFingerLike(infectButtonRT);

        // 감염 성공 전까지 반복
        while (NPCManager.Instance.GreenZombies.Count == 0)
        {
            Vector2 from = infectButtonRT.anchoredPosition;
            Vector2 to = WorldToCanvasAnchored(targetCitizen.transform.position);

            yield return PlayFingerDragAnchored(from, to);

            yield return new WaitForSeconds(0.4f);
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

        if (fingerPrefab == null ||
            touchCirclePrefab == null ||
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
        GameObject circle = Instantiate(touchCirclePrefab, canvas.transform);
        RectTransform circleRT = circle.GetComponent<RectTransform>();
        CopyRectLike(speedRT, circleRT);
        circleRT.anchoredPosition = speedRT.anchoredPosition;
        circleRT.localScale = Vector3.one;

        // 손가락도 버튼 위에 고정 스폰
        SpawnFingerLike(speedRT);

        Game.Instance.SetGameSpeed(1f);

        yield return new WaitUntil(() => Game.Instance.GameSpeed > 1f);

        Destroy(circle);
        ClearFinger();

        Debug.Log("[Tutorial] SPEED COMPLETE");
    }

    // =====================================================
    // UI Helpers
    // =====================================================
    void SpawnFingerLike(RectTransform targetButtonRT)
    {
        ClearFinger();

        fingerInstance = Instantiate(fingerPrefab, canvas.transform);
        fingerRT = fingerInstance.GetComponent<RectTransform>();

        CopyRectLike(targetButtonRT, fingerRT);
        fingerRT.pivot = new Vector2(0.5f, 1f);   // 손끝 기준
        fingerRT.anchoredPosition = targetButtonRT.anchoredPosition;
        fingerRT.localScale = Vector3.one;
        fingerRT.gameObject.SetActive(true);
    }

    IEnumerator PlayFingerDragAnchored(Vector2 from, Vector2 to)
    {
        if (fingerRT == null)
            yield break;

        fingerRT.gameObject.SetActive(true);
        fingerRT.anchoredPosition = from;

        float duration = 0.7f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            fingerRT.anchoredPosition = Vector2.Lerp(from, to, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.15f);
        fingerRT.gameObject.SetActive(false);
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
        dst.pivot = src.pivot;
        dst.sizeDelta = src.sizeDelta;
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
}
