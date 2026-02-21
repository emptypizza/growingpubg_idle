using UnityEngine;

/// <summary>
/// 카메라 컨트롤러. 플레이어 추적 + 고도에 따른 줌.
/// JS camera 객체의 zoom/position 로직을 Cinemachine 없이 구현.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float baseCameraSize = 6f;
    [SerializeField] private float maxZoomOut = 50f;
    [SerializeField] private float zoomDivisor = 30f; // altitude / zoomDivisor → 추가 줌아웃

    private Transform target;
    private EntityBase targetEntity;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    public void SetTarget(Transform t, EntityBase entity)
    {
        target = t;
        targetEntity = entity;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // ── 위치 추적 ──
        Vector3 desiredPos = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // ── 줌 (고도에 따라) ──
        if (cam != null && targetEntity != null)
        {
            float altitudeZoom = targetEntity.altitude / zoomDivisor;
            float targetSize = baseCameraSize + altitudeZoom;
            targetSize = Mathf.Clamp(targetSize, baseCameraSize, maxZoomOut);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, smoothSpeed * Time.deltaTime);
        }
    }
}
