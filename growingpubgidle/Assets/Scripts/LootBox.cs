using UnityEngine;

/// <summary>
/// 보급 상자. JS LootBox 클래스 변환.
/// 트리거 영역에 엔티티가 진입하면 무기 지급.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class LootBox : MonoBehaviour
{
    public bool isActive = true;
    public WeaponType weaponType;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 랜덤 무기 결정
        float r = Random.value;
        if (r < 0.6f) weaponType = WeaponType.Gun;
        else if (r < 0.8f) weaponType = WeaponType.Shield;
        else weaponType = WeaponType.Knife;

        // 콜라이더를 트리거로 설정
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        EntityBase entity = other.GetComponent<EntityBase>();
        if (entity != null && entity.alive)
        {
            entity.EquipWeapon(weaponType);
            UIManager.Instance?.AddLog($"Acquired {WeaponData.Get(weaponType).displayName}!");
            isActive = false;
            ParticleSpawner.Spawn(transform.position, new Color(1f, 0.84f, 0f), 10); // 금색
            Destroy(gameObject);
        }
    }
}
