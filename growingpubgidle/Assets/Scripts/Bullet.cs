using UnityEngine;

/// <summary>
/// 총알 스크립트. JS Bullet 클래스를 Unity 물리 기반으로 변환.
/// Rigidbody2D velocity로 이동, OnTriggerEnter2D로 충돌 감지.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bullet : MonoBehaviour
{
    private int ownerId;
    private float damage;
    private float lifeTime = 1.0f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(float angle, int owner, float dmg)
    {
        ownerId = owner;
        damage = dmg;

        WeaponData wData = WeaponData.Get(WeaponType.Gun);
        Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * wData.bulletSpeed;
        rb.linearVelocity = velocity;

        // 발사 방향으로 회전
        transform.rotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 맵 밖으로 나가면 파괴
        Vector2 pos = rb.position;
        if (pos.x < 0f || pos.x > GameConfig.MAP_SIZE || pos.y < 0f || pos.y > GameConfig.MAP_SIZE)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 장애물에 충돌
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            ParticleSpawner.Spawn(transform.position, Color.gray, 3);
            Destroy(gameObject);
            return;
        }

        // 엔티티에 충돌
        EntityBase target = other.GetComponent<EntityBase>();
        if (target != null && target.entityId != ownerId && target.alive)
        {
            EntityBase attacker = FindOwner();
            target.TakeDamage(damage, attacker);
            ParticleSpawner.Spawn(target.transform.position, Color.red, 3);
            Destroy(gameObject);
        }
    }

    private EntityBase FindOwner()
    {
        foreach (var e in GameManager.Instance.GetAllEntities())
        {
            if (e.entityId == ownerId) return e;
        }
        return null;
    }
}
