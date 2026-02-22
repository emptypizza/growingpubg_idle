using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 UI. JS drawMinimap() 함수를 Unity UI 오버레이로 변환.
/// RawImage + RenderTexture 방식 또는 UI 요소 직접 배치.
/// 여기서는 간단한 UI 요소 방식을 사용.
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private float minimapSize = 150f;

    [Header("Icons")]
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform portalIcon;
    [SerializeField] private Image blueZoneCircle;

    private float scale;

    private void Start()
    {
        scale = minimapSize / GameConfig.MAP_SIZE;
    }

    private void LateUpdate()
    {
        if (GameManager.Instance == null) return;

        EntityBase player = GameManager.Instance.GetPlayer();
        if (player == null || !player.alive) return;

        // 플레이어 위치
        if (playerIcon != null)
        {
            playerIcon.anchoredPosition = new Vector2(
                player.transform.position.x * scale,
                player.transform.position.y * scale);
        }

        // 블루존
        if (blueZoneCircle != null && GameManager.Instance.blueZone != null)
        {
            BlueZone zone = GameManager.Instance.blueZone;
            Vector2 center = zone.GetCurrentCenter();
            float radius = zone.GetCurrentRadius();

            blueZoneCircle.rectTransform.anchoredPosition = new Vector2(
                center.x * scale, center.y * scale);
            float diameter = radius * 2f * scale;
            blueZoneCircle.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        }

        // 포탈
        if (portalIcon != null)
        {
            Portal portal = FindAnyObjectByType<Portal>();
            if (portal != null)
            {
                portalIcon.gameObject.SetActive(true);
                portalIcon.anchoredPosition = new Vector2(
                    portal.transform.position.x * scale,
                    portal.transform.position.y * scale);
            }
            else
            {
                portalIcon.gameObject.SetActive(false);
            }
        }
    }
}
