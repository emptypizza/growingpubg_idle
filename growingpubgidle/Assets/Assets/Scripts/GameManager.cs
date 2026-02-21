using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 게임 전체 상태를 관리하는 싱글톤 매니저.
/// JS의 전역 상태(gameState, initGame, update 등)를 C#으로 변환.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject lootBoxPrefab;
    [SerializeField] private GameObject portalPrefab;

    [Header("State")]
    public GameState currentState = GameState.Menu;
    public float gameTime;

    // 엔티티 관리
    private List<EntityBase> allEntities = new List<EntityBase>();
    private List<LootBox> lootBoxes = new List<LootBox>();
    private List<Bullet> bullets = new List<Bullet>();

    // 내부 타이머
    private float lootSpawnTimer;
    private bool hasFirstLanded;

    // 포탈
    private Portal activePortal;
    private float portalSpawnTimer;

    // 블루존 참조
    [HideInInspector] public BlueZone blueZone;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        blueZone = FindAnyObjectByType<BlueZone>();
        // 메뉴 상태로 시작 — UI가 시작 버튼을 보여줌
        currentState = GameState.Menu;
    }

    private void Update()
    {
        if (currentState == GameState.Menu || currentState == GameState.End) return;

        float dt = Time.deltaTime;

        // ── 쿨다운 업데이트 ──
        foreach (var entity in allEntities)
        {
            if (entity != null && entity.alive && entity.attackCooldown > 0f)
            {
                entity.attackCooldown -= dt;
            }
        }

        // ── 보급 스폰 ──
        if (hasFirstLanded)
        {
            lootSpawnTimer -= dt;
            if (lootSpawnTimer <= 0f)
            {
                SpawnRandomLootBox();
                lootSpawnTimer = GameConfig.LOOT_SPAWN_INTERVAL;
            }

            // ── 포탈 관리 ──
            UpdatePortal(dt);
        }

        // ── 상태별 업데이트 ──
        if (currentState == GameState.Dropping)
        {
            UpdateDropping();
        }
        else if (currentState == GameState.Ground)
        {
            UpdateGround(dt);
        }

        // ── UI 업데이트 ──
        UpdateUI();
    }

    // ═══════════════════════════════════════════
    //  게임 초기화
    // ═══════════════════════════════════════════

    /// <summary>
    /// UI의 "게임 시작" 버튼에서 호출
    /// </summary>
    public void StartGame()
    {
        ClearAll();

        hasFirstLanded = false;
        lootSpawnTimer = GameConfig.LOOT_SPAWN_INTERVAL;
        portalSpawnTimer = GameConfig.PORTAL_SPAWN_TIME;

        // 맵 오브젝트 생성
        MapGenerator.Instance?.GenerateMap();

        // 엔티티 스폰
        for (int i = 0; i < GameConfig.TOTAL_ENTITIES; i++)
        {
            bool isPlayer = (i == 0);
            GameObject prefab = isPlayer ? playerPrefab : botPrefab;
            Vector2 pos = GetSafeSpawnPoint();
            GameObject go = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
            EntityBase entity = go.GetComponent<EntityBase>();
            entity.InitEntity(i, isPlayer);
            allEntities.Add(entity);

            if (isPlayer)
            {
                // 카메라 타겟 설정
                CameraController cam = FindAnyObjectByType<CameraController>();
                if (cam != null) cam.SetTarget(go.transform, entity);
            }
        }

        // 초기 보급상자 30개
        for (int i = 0; i < GameConfig.INITIAL_LOOT_COUNT; i++)
        {
            SpawnRandomLootBox();
        }

        currentState = GameState.Dropping;
        gameTime = GameConfig.DROP_TIME_NORMAL;

        UIManager.Instance?.OnGameStart();
    }

    // ═══════════════════════════════════════════
    //  강하 단계
    // ═══════════════════════════════════════════

    private void UpdateDropping()
    {
        bool allLanded = true;
        foreach (var e in allEntities)
        {
            if (e != null && e.alive && e.altitude > 0f)
            {
                allLanded = false;
                break;
            }
        }

        if (!hasFirstLanded)
        {
            // 첫 착지 체크
            foreach (var e in allEntities)
            {
                if (e != null && e.altitude <= 0f)
                {
                    hasFirstLanded = true;
                    UIManager.Instance?.AddLog("First landing! Blue zone shrinking, supply drops starting.");
                    break;
                }
            }
        }

        if (allLanded)
        {
            StartGroundPhase();
        }
    }

    // ═══════════════════════════════════════════
    //  지상 전투 단계
    // ═══════════════════════════════════════════

    private void StartGroundPhase()
    {
        currentState = GameState.Ground;
        gameTime = GameConfig.GROUND_TIME;

        // 블루존 시작
        if (blueZone != null)
        {
            blueZone.InitZone();
        }

        UIManager.Instance?.OnGroundPhase();
        UIManager.Instance?.AddLog("Blue zone activated! Move to the safe zone!");
    }

    private void UpdateGround(float dt)
    {
        gameTime -= dt;
        if (gameTime <= 0f)
        {
            gameTime = 0f;
            EndGame(true, false, null);
        }

        // 블루존 업데이트
        if (blueZone != null)
        {
            blueZone.UpdateZone(dt, gameTime);

            // 모든 엔티티에 자기장 데미지
            foreach (var e in allEntities)
            {
                if (e != null && e.alive && e.altitude <= 0f)
                {
                    if (!blueZone.IsInsideSafeZone(e.transform.position))
                    {
                        e.TakeDamage(GameConfig.BLUEZONE_DAMAGE * dt, null);
                    }
                }
            }
        }
    }

    // ═══════════════════════════════════════════
    //  포탈
    // ═══════════════════════════════════════════

    private void UpdatePortal(float dt)
    {
        if (activePortal == null)
        {
            portalSpawnTimer -= dt;
            if (portalSpawnTimer <= 0f)
            {
                SpawnPortal();
                portalSpawnTimer = GameConfig.PORTAL_SPAWN_TIME + Random.Range(0f, 20f);
            }
        }
        else
        {
            // 포탈이 만료되면 Portal.cs에서 자체 파괴
            if (activePortal == null)
            {
                UIManager.Instance?.AddLog("The wormhole portal has disappeared.");
            }
        }
    }

    private void SpawnPortal()
    {
        if (portalPrefab == null) return;
        Vector2 pos = GetSafeSpawnPoint();
        GameObject go = Instantiate(portalPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
        activePortal = go.GetComponent<Portal>();
        if (activePortal != null)
        {
            activePortal.Init(GameConfig.PORTAL_DURATION);
        }
        UIManager.Instance?.AddLog("A green wormhole portal has appeared!");
    }

    // ═══════════════════════════════════════════
    //  승리/패배
    // ═══════════════════════════════════════════

    public void CheckWinCondition()
    {
        EntityBase player = GetPlayer();
        if (player == null) return;

        int aliveCount = 0;
        foreach (var e in allEntities)
        {
            if (e != null && e.alive) aliveCount++;
        }

        if (!player.alive)
        {
            EndGame(false, false, null);
        }
        else if (aliveCount == 1)
        {
            EndGame(true, true, player);
        }
    }

    public void EndGame(bool win, bool allDead, EntityBase winner)
    {
        currentState = GameState.End;
        UIManager.Instance?.OnGameEnd(win, allDead, winner);
    }

    /// <summary>
    /// 포탈을 통한 승리 (Portal.cs에서 호출)
    /// </summary>
    public void PortalEscape(EntityBase entity)
    {
        EndGame(true, false, entity);
    }

    // ═══════════════════════════════════════════
    //  스폰 헬퍼
    // ═══════════════════════════════════════════

    public void SpawnBullet(Vector2 origin, float angle, int ownerId, float damage)
    {
        if (bulletPrefab == null) return;
        GameObject go = Instantiate(bulletPrefab, new Vector3(origin.x, origin.y, 0f), Quaternion.identity);
        Bullet bullet = go.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Init(angle, ownerId, damage);
        }
    }

    private void SpawnRandomLootBox()
    {
        if (lootBoxPrefab == null) return;
        Vector2 pos = GetSafeSpawnPoint();
        GameObject go = Instantiate(lootBoxPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
        LootBox box = go.GetComponent<LootBox>();
        if (box != null)
        {
            lootBoxes.Add(box);
        }
    }

    public Vector2 GetSafeSpawnPoint()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            float rx = Random.Range(5f, GameConfig.MAP_SIZE - 5f);
            float ry = Random.Range(5f, GameConfig.MAP_SIZE - 5f);
            Vector2 point = new Vector2(rx, ry);

            Collider2D hit = Physics2D.OverlapCircle(point, GameConfig.PLAYER_RADIUS + 0.5f);
            if (hit == null)
            {
                return point;
            }
        }
        // 폴백
        return new Vector2(GameConfig.MAP_SIZE / 2f, GameConfig.MAP_SIZE / 2f);
    }

    // ═══════════════════════════════════════════
    //  유틸리티
    // ═══════════════════════════════════════════

    public EntityBase[] GetAllEntities()
    {
        return allEntities.ToArray();
    }

    public EntityBase GetPlayer()
    {
        foreach (var e in allEntities)
        {
            if (e != null && e.isPlayer) return e;
        }
        return null;
    }

    private void UpdateUI()
    {
        if (UIManager.Instance == null) return;

        EntityBase player = GetPlayer();
        if (player == null) return;

        int aliveCount = 0;
        foreach (var e in allEntities) { if (e != null && e.alive) aliveCount++; }

        UIManager.Instance.UpdateHUD(
            gameTime,
            aliveCount,
            player.kills,
            player.altitude,
            player.weapon,
            player.ammo,
            player.lootCount
        );
    }

    private void ClearAll()
    {
        // 기존 엔티티 파괴
        foreach (var e in allEntities) { if (e != null) Destroy(e.gameObject); }
        allEntities.Clear();

        foreach (var b in lootBoxes) { if (b != null) Destroy(b.gameObject); }
        lootBoxes.Clear();

        if (activePortal != null) { Destroy(activePortal.gameObject); activePortal = null; }

        // 기존 총알 파괴
        Bullet[] existingBullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
        foreach (var b in existingBullets) Destroy(b.gameObject);

        MapGenerator.Instance?.ClearMap();
    }
}
