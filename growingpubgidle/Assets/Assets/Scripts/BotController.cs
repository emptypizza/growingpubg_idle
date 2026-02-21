using UnityEngine;

/// <summary>
/// AI 봇 행동 스크립트.
/// JS Entity.updateAI()를 C#으로 변환. FSM + 7방향 장애물 회피.
/// </summary>
[RequireComponent(typeof(EntityBase))]
[RequireComponent(typeof(Rigidbody2D))]
public class BotController : MonoBehaviour
{
    private EntityBase entity;
    private Rigidbody2D rb;

    // AI 상태
    private float actionTimer;
    private float stuckCounter;
    private Vector2 lastPosition;

    // 장애물 회피 각도 오프셋 (라디안)
    private static readonly float[] CheckAngles = { 0f, 0.52f, -0.52f, 1.05f, -1.05f, 1.57f, -1.57f };

    private void Awake()
    {
        entity = GetComponent<EntityBase>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!entity.alive) return;

        if (GameManager.Instance.currentState == GameState.Menu
            || GameManager.Instance.currentState == GameState.End) return;

        if (entity.altitude > 0f)
        {
            UpdateDrop();
        }
        else
        {
            UpdateAI();
        }
    }

    // ═══════════════════════════════════════════
    //  강하 (AI)
    // ═══════════════════════════════════════════

    private void UpdateDrop()
    {
        float dt = Time.deltaTime;

        // 랜덤 다이빙
        if (actionTimer <= 0f)
        {
            entity.isDiving = Random.value > 0.5f;
            actionTimer = Random.Range(0.5f, 2.5f);
        }
        else
        {
            actionTimer -= dt;
        }

        float rate = entity.dropSpeed;
        if (entity.isDiving)
        {
            rate = GameConfig.START_ALTITUDE / GameConfig.DROP_TIME_FAST;
        }
        entity.altitude -= rate * dt;

        // 공중 랜덤 이동
        Vector2 randomDir = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
        if (randomDir.sqrMagnitude > 0.01f)
        {
            rb.MovePosition(rb.position + randomDir.normalized * GameConfig.PLAYER_SPEED_AIR * dt);
        }

        ClampToMap();

        // 착지
        if (entity.altitude <= 0f)
        {
            entity.altitude = 0f;
            entity.isDiving = false;
            ParticleSpawner.Spawn(transform.position, Color.grey, 20);
        }
    }

    // ═══════════════════════════════════════════
    //  지상 AI
    // ═══════════════════════════════════════════

    private void UpdateAI()
    {
        float dt = Time.deltaTime;
        if (actionTimer > 0f) actionTimer -= dt;

        // ── 끼임 감지 ──
        float movedDist = Vector2.Distance(rb.position, lastPosition);
        if (movedDist < 0.1f)
        {
            stuckCounter += dt;
        }
        else
        {
            stuckCounter = 0f;
        }
        lastPosition = rb.position;

        // 끼임 탈출
        if (stuckCounter > 0.5f)
        {
            if (stuckCounter > 2f) stuckCounter = 0f;
            float escapeAngle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 escapeDir = new Vector2(Mathf.Cos(escapeAngle), Mathf.Sin(escapeAngle));
            rb.MovePosition(rb.position + escapeDir * 5f);
            return;
        }

        BlueZone zone = GameManager.Instance.blueZone;

        // ── 우선순위 1: 자기장 밖이면 안전지대로 이동 ──
        if (zone != null && GameManager.Instance.currentState == GameState.Ground)
        {
            float distToCenter = Vector2.Distance(rb.position, zone.GetCurrentCenter());
            if (distToCenter > zone.GetCurrentRadius() - 10f)
            {
                MoveToward(zone.GetCurrentCenter(), dt);
                TryAttackNearby(dt, true);
                return;
            }
        }

        // ── 우선순위 2: 무기가 없으면 보급상자 탐색 ──
        if (entity.weapon == WeaponType.None ||
            (entity.weapon == WeaponType.Gun && entity.ammo <= 0))
        {
            LootBox nearestBox = FindNearestLootBox();
            if (nearestBox != null)
            {
                MoveToward(nearestBox.transform.position, dt);
                entity.aimAngle = Mathf.Atan2(
                    nearestBox.transform.position.y - transform.position.y,
                    nearestBox.transform.position.x - transform.position.x);
                return;
            }
        }

        // ── 우선순위 3: 적 추적 + 공격 ──
        TryAttackNearby(dt, false);
    }

    private void TryAttackNearby(float dt, bool onlyAttack)
    {
        EntityBase target = FindNearestEnemy();
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        WeaponData wData = WeaponData.Get(entity.weapon);

        // 조준
        entity.aimAngle = Mathf.Atan2(
            target.transform.position.y - transform.position.y,
            target.transform.position.x - transform.position.x);
        transform.rotation = Quaternion.Euler(0f, 0f, entity.aimAngle * Mathf.Rad2Deg);

        if (!onlyAttack)
        {
            float desired = wData.range * 0.8f;
            if (entity.weapon == WeaponType.Gun) desired = 25f;

            if (dist > desired)
            {
                MoveToward(target.transform.position, dt);
            }
            else if (entity.weapon == WeaponType.Gun && dist < 10f)
            {
                // 후퇴
                Vector2 retreatDir = ((Vector2)transform.position - (Vector2)target.transform.position).normalized;
                MoveInDirection(retreatDir, dt);
            }
        }

        // 사거리 내이면 공격
        float effectiveRange = (entity.weapon == WeaponType.Gun) ? 60f : wData.range;
        if (dist <= effectiveRange)
        {
            entity.Attack();
        }
    }

    // ═══════════════════════════════════════════
    //  이동 + 장애물 회피 (7방향 레이캐스트)
    // ═══════════════════════════════════════════

    private void MoveToward(Vector2 target, float dt)
    {
        Vector2 dir = (target - rb.position).normalized;
        MoveInDirection(dir, dt);
    }

    private void MoveInDirection(Vector2 dir, float dt)
    {
        float speed = GameConfig.PLAYER_SPEED_GROUND;
        float baseAngle = Mathf.Atan2(dir.y, dir.x);
        float checkDist = speed * dt + GameConfig.PLAYER_RADIUS + 0.5f;

        foreach (float offset in CheckAngles)
        {
            float testAngle = baseAngle + offset;
            Vector2 testDir = new Vector2(Mathf.Cos(testAngle), Mathf.Sin(testAngle));
            Vector2 nextPos = rb.position + testDir * speed * dt;

            // 레이캐스트로 충돌 체크
            RaycastHit2D hit = Physics2D.CircleCast(rb.position, GameConfig.PLAYER_RADIUS * 0.8f, testDir, checkDist, LayerMask.GetMask("Obstacle"));

            if (hit.collider == null)
            {
                rb.MovePosition(nextPos);
                ClampToMap();
                return;
            }
        }
        // 모든 방향이 막혀있으면 이동하지 않음 (stuckCounter가 처리)
    }

    // ═══════════════════════════════════════════
    //  탐색 헬퍼
    // ═══════════════════════════════════════════

    private EntityBase FindNearestEnemy()
    {
        EntityBase nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var e in GameManager.Instance.GetAllEntities())
        {
            if (e == entity || !e.alive) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = e;
            }
        }
        return nearest;
    }

    private LootBox FindNearestLootBox()
    {
        LootBox nearest = null;
        float minDist = Mathf.Infinity;

        LootBox[] boxes = FindObjectsByType<LootBox>(FindObjectsSortMode.None);
        foreach (var box in boxes)
        {
            if (!box.isActive) continue;
            float d = Vector2.Distance(transform.position, box.transform.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = box;
            }
        }
        return nearest;
    }

    private void ClampToMap()
    {
        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, 0f, GameConfig.MAP_SIZE);
        pos.y = Mathf.Clamp(pos.y, 0f, GameConfig.MAP_SIZE);
        rb.position = pos;
    }
}
