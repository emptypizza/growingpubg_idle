using UnityEngine;

/// <summary>
/// 웜홀 포탈. JS portal 객체를 컴포넌트로 변환.
/// 7개 이상 파밍한 플레이어가 진입하면 포탈 탈출 승리.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class Portal : MonoBehaviour
{
    private float remainingTime;
    private bool active;

    [Header("Visual")]
    [SerializeField] private float rotateSpeed = 90f;

    public void Init(float duration)
    {
        remainingTime = duration;
        active = true;

        // 콜라이더를 트리거로
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = GameConfig.PORTAL_RADIUS;
        }
    }

    private void Update()
    {
        if (!active) return;

        // 회전 애니메이션
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // 타이머
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            active = false;
            UIManager.Instance?.AddLog("웜홀 포탈이 사라졌습니다.");
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        EntityBase entity = other.GetComponent<EntityBase>();
        if (entity != null && entity.isPlayer && entity.alive)
        {
            // 7개 이상 파밍했으면 탈출 승리
            if (entity.lootCount >= GameConfig.WIN_LOOT_COUNT)
            {
                GameManager.Instance?.PortalEscape(entity);
            }
        }
    }
}
