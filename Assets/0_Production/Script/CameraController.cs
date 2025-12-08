using UnityEngine;
public class CameraController : MonoBehaviour
{
    public float moveSpeed = 0.1f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 5;
    public float maxZoom = 25;

    private Camera cam;

    [Header("Camera Bounds")]
    public float minX = -20f;
    public float maxX = 20f;
    public float minZ = -20f;
    public float maxZ = 20f;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (GameManager.Instance.controlMode != ControlMode.Camera)
            return;

        HandleMove();
        HandleZoom();

        ClampPosition();
    }

    void ClampPosition()
    {
        float distance = 10f; // 카메라와 실제 바라보는 지점의 거리(필요하면 변수화)

        // 카메라가 바라보는 지점 계산
        Vector3 center = transform.position + transform.forward * distance;

        // Clamp는 center에 적용
        center.x = Mathf.Clamp(center.x, minX, maxX);
        center.z = Mathf.Clamp(center.z, minZ, maxZ);

        // 다시 카메라 위치 계산
        transform.position = center - transform.forward * distance;
    }


    void HandleMove()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * moveSpeed;

            transform.position += move;
        }

        lastMousePos = Input.mousePosition;
    }

    void HandleZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float prevDist = (t0.position - t1.position - t0.deltaPosition - t1.deltaPosition).magnitude;
            float currDist = (t0.position - t1.position).magnitude;

            float diff = currDist - prevDist;

            cam.orthographicSize -= diff * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    Vector3 lastMousePos;
}
