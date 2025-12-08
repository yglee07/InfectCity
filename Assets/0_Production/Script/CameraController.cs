using UnityEngine;
public class CameraController : MonoBehaviour
{
    [Header("Move")]
    public float dragSpeed = 0.02f;

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


    private Vector3 lastPos;

    public Transform cameraPos;
    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (GameManager.Instance.controlMode != ControlMode.Camera)
            return;

        HandleDrag();
        HandleZoom();

        ClampPosition();
    }

    // -------------------------
    // 자연스러운 드래그 이동
    // -------------------------
    void HandleDrag()
    {
#if UNITY_EDITOR
        // 마우스용
        if (Input.GetMouseButtonDown(0))
            lastPos = GetWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {
            Vector3 newPos = GetWorldPoint(Input.mousePosition);
            Vector3 diff = lastPos - newPos;
            transform.position += diff;
        }
#else
        // 터치용
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
                lastPos = GetWorldPoint(t.position);

            if (t.phase == TouchPhase.Moved)
            {
                Vector3 newPos = GetWorldPoint(t.position);
                Vector3 diff = lastPos - newPos;
                transform.position += diff;
            }
        }
#endif
    }

    // 화면 → 월드 평면 위치 변환
    Vector3 GetWorldPoint(Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(screen);

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        // y=0 지면 기준

        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return transform.position;
    }

    // -------------------------
    // 정확한 핀치 줌
    // -------------------------
    void HandleZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float prevDist = (t0.position - t0.deltaPosition - (t1.position - t1.deltaPosition)).magnitude;
            float currDist = (t0.position - t1.position).magnitude;

            float diff = (currDist - prevDist) * zoomSpeed;

            // 🔥 Perspective 줌은 카메라 위치 이동!
            Vector3 dir = cam.transform.forward;     // 카메라가 바라보는 방향
            cam.transform.position += dir * diff;    // 확대(가까이), 축소(멀리)

            // 🔥 줌 범위 제한 (카메라 높이 기준)
            float height = cam.transform.position.y;
            height = Mathf.Clamp(height, minZoom, maxZoom);
            cam.transform.position = new Vector3(cam.transform.position.x, height, cam.transform.position.z);
        }
    }

    // -------------------------
    // Clamp 카메라 위치
    // -------------------------
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

        cam.transform.position = cameraPos.position;
        cam.transform.rotation = cameraPos.rotation;
    }
}
