using UnityEngine;

/// <summary>
/// 자기장(블루존) 시스템. JS safeZone 객체를 컴포넌트로 변환.
/// 원형 안전지대가 시간에 따라 축소.
/// </summary>
public class BlueZone : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private LineRenderer circleRenderer;
    [SerializeField] private int circleSegments = 64;

    // 안전지대 파라미터
    private Vector2 startCenter;
    private Vector2 endCenter;
    private Vector2 currentCenter;
    private float startRadius;
    private float endRadius;
    private float currentRadius;

    private bool initialized;

    public void InitZone()
    {
        startCenter = new Vector2(GameConfig.MAP_SIZE / 2f, GameConfig.MAP_SIZE / 2f);
        Vector2 endPos = GameManager.Instance.GetSafeSpawnPoint();
        endCenter = endPos;
        currentCenter = startCenter;

        startRadius = GameConfig.MAP_SIZE * 0.8f;
        endRadius = 20f;
        currentRadius = startRadius;

        initialized = true;
        UpdateVisual();
    }

    public void UpdateZone(float dt, float remainingTime)
    {
        if (!initialized) return;

        float progress = 1f - (remainingTime / GameConfig.GROUND_TIME);
        progress = Mathf.Clamp01(progress);

        currentRadius = Mathf.Lerp(startRadius, endRadius, progress);
        currentCenter = Vector2.Lerp(startCenter, endCenter, progress);

        UpdateVisual();
    }

    public bool IsInsideSafeZone(Vector2 position)
    {
        if (!initialized) return true;
        return Vector2.Distance(position, currentCenter) <= currentRadius;
    }

    public Vector2 GetCurrentCenter() => currentCenter;
    public float GetCurrentRadius() => currentRadius;

    private void UpdateVisual()
    {
        if (circleRenderer == null) return;

        circleRenderer.positionCount = circleSegments + 1;
        circleRenderer.loop = true;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            float x = currentCenter.x + Mathf.Cos(angle) * currentRadius;
            float y = currentCenter.y + Mathf.Sin(angle) * currentRadius;
            circleRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}
