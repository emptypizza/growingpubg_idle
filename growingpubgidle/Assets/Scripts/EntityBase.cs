using UnityEngine;

/// <summary>
/// 플레이어와 봇이 공유하는 기본 엔티티 컴포넌트.
/// JS Entity 클래스의 공통 필드와 메서드를 C#으로 변환.
/// </summary>
public class EntityBase : MonoBehaviour
{
    [Header("Identity")]
    public int entityId;
    public bool isPlayer;
    public string displayName;

    [Header("Stats")]
    public float maxHp;
    public float hp;
    public float stamina = 100f;
    public bool fatigued;
    public bool alive = true;

    [Header("Weapon")]
    public WeaponType weapon = WeaponType.None;
    public int ammo;
    public int kills;
    public int lootCount;
    public float attackCooldown;

    [Header("Drop")]
    public float altitude;
    public bool isDiving;
    public float dropSpeed;

    [Header("Combat")]
    public float aimAngle; // 라디안

    // 컴포넌트 캐시
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public SpriteRenderer spriteRenderer;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public virtual void InitEntity(int id, bool player)
    {
        entityId = id;
        isPlayer = player;
        displayName = player ? "Me" : $"BOT {id}";
        maxHp = player ? GameConfig.PLAYER_MAX_HP : GameConfig.BOT_MAX_HP;
        hp = maxHp;
        stamina = 100f;
        fatigued = false;
        alive = true;
        weapon = WeaponType.None;
        ammo = 0;
        kills = 0;
        lootCount = 0;
        attackCooldown = 0f;
        altitude = GameConfig.START_ALTITUDE;
        isDiving = false;
        dropSpeed = GameConfig.START_ALTITUDE / GameConfig.DROP_TIME_NORMAL;
    }

    /// <summary>
    /// 데미지를 받음. attacker가 null이면 환경 데미지(자기장 등).
    /// </summary>
    public void TakeDamage(float amount, EntityBase attacker)
    {
        if (!alive) return;

        // 방패 장착 시 데미지 감소
        if (weapon == WeaponType.Shield)
        {
            amount *= WeaponData.Get(WeaponType.Shield).defense;
        }

        hp -= amount;

        if (isPlayer)
        {
            UIManager.Instance?.UpdateHPBar(hp, maxHp);
        }

        if (hp <= 0f && alive)
        {
            hp = 0f;
            alive = false;
            // 킬 카운트 증가
            if (attacker != null)
            {
                attacker.kills++;
            }
            // 파티클 효과
            ParticleSpawner.Spawn(transform.position, spriteRenderer != null ? spriteRenderer.color : Color.red, 20);
            // 로그
            string attackerName = attacker != null ? (attacker.isPlayer ? "You" : "BOT") : "Someone";
            string victimName = isPlayer ? "you" : "enemy";
            UIManager.Instance?.AddLog($"{attackerName} killed {victimName}");
            // 승리 조건 체크
            GameManager.Instance?.CheckWinCondition();
            // 사망 처리 (비활성화 또는 시각 변경)
            OnDeath();
        }
    }

    protected virtual void OnDeath()
    {
        // 오버라이드 가능
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.36f, 0.25f, 0.22f, 0.7f); // 갈색 톤
        }
        if (rb != null)
        {
            rb.simulated = false;
        }
    }

    /// <summary>
    /// 무기 획득
    /// </summary>
    public void EquipWeapon(WeaponType type)
    {
        lootCount++;

        // 각성 체크 (7개 이상 파밍)
        if (isPlayer && lootCount == GameConfig.WIN_LOOT_COUNT)
        {
            UIManager.Instance?.AddLog("Awakened! Find the green portal to escape!");
        }

        weapon = type;
        ammo = (type == WeaponType.Gun) ? 30 : 0;

        if (isPlayer)
        {
            string msg = $"Got: {WeaponData.Get(type).displayName}";
            if (type == WeaponType.Gun) msg += " (30 rounds)";
            UIManager.Instance?.AddLog(msg);
        }
    }

    /// <summary>
    /// 근접 공격 수행
    /// </summary>
    public void PerformMeleeAttack()
    {
        if (!alive) return;

        WeaponData wData = WeaponData.Get(weapon);
        attackCooldown = wData.delay;

        // 범위 내 적에게 데미지
        EntityBase[] allEntities = GameManager.Instance.GetAllEntities();
        foreach (var target in allEntities)
        {
            if (target == this || !target.alive) continue;
            float dist = Vector2.Distance(transform.position, target.transform.position);
            if (dist < wData.range)
            {
                // 각도 체크 (전방 90도 범위)
                float angleToTarget = Mathf.Atan2(
                    target.transform.position.y - transform.position.y,
                    target.transform.position.x - transform.position.x);
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(aimAngle * Mathf.Rad2Deg, angleToTarget * Mathf.Rad2Deg));
                if (angleDiff < 85f)
                {
                    target.TakeDamage(wData.damage, this);
                    ParticleSpawner.Spawn(target.transform.position, Color.red, 5);
                }
            }
        }
    }

    /// <summary>
    /// 총알 발사
    /// </summary>
    public void FireGun()
    {
        if (!alive) return;

        WeaponData wData = WeaponData.Get(WeaponType.Gun);
        attackCooldown = wData.delay;

        if (ammo > 0)
        {
            ammo--;
            GameManager.Instance?.SpawnBullet(transform.position, aimAngle, entityId, wData.damage);
        }
    }

    /// <summary>
    /// 공격 실행 (무기에 따라 분기)
    /// </summary>
    public void Attack()
    {
        if (!alive || attackCooldown > 0f) return;

        if (weapon == WeaponType.Gun)
        {
            FireGun();
        }
        else
        {
            PerformMeleeAttack();
        }
    }
}
