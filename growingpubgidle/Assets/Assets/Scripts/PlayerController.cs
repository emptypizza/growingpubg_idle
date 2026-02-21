using UnityEngine;

/// <summary>
/// 플레이어 제어 스크립트.
/// JS Entity의 isPlayer 분기 로직을 독립 컴포넌트로 분리.
/// 강하, 이동, 스프린트, 스테미나, 조준, 공격을 처리.
/// </summary>
[RequireComponent(typeof(EntityBase))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private EntityBase entity;
    private Rigidbody2D rb;
    private Camera mainCam;

    // 입력
    private MobileInput mobileInput;
    private Vector2 moveInput;
    private Vector2 aimWorldPos;
    private bool isSprinting;
    private bool isAttacking;

    private void Awake()
    {
        entity = GetComponent<EntityBase>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        mainCam = Camera.main;
        mobileInput = FindAnyObjectByType<MobileInput>();
    }

    private void Update()
    {
        if (!entity.alive || GameManager.Instance.currentState == GameState.Menu
            || GameManager.Instance.currentState == GameState.End) return;

        GatherInput();

        if (entity.altitude > 0f)
        {
            UpdateDrop();
        }
        else
        {
            UpdateGround();
        }
    }

    private void GatherInput()
    {
        // ── 키보드 입력 (에디터/PC) ──
        float kx = 0f, ky = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ky += 2f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ky -= 2f    ;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kx -= 2f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kx += 2f;

        moveInput = new Vector2(kx, ky);

        // 모바일 조이스틱 오버라이드
        if (mobileInput != null && mobileInput.HasMoveInput)
        {
            moveInput = mobileInput.MoveDirection;
        }

        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        // 스프린트
        isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (mobileInput != null) isSprinting |= mobileInput.IsSprinting;

        // 공격
        isAttacking = Input.GetMouseButton(0);
        if (mobileInput != null) isAttacking |= mobileInput.IsAttacking;

        // 조준 방향 (마우스 또는 터치)
        if (mainCam != null)
        {
            if (mobileInput != null && mobileInput.HasAimInput)
            {
                aimWorldPos = (Vector2)transform.position + mobileInput.AimDirection * 20f;
            }
            else
            {
                aimWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
            }
        }
    }

    // ═══════════════════════════════════════════
    //  강하 단계
    // ═══════════════════════════════════════════

    private void UpdateDrop()
    {
        float dt = Time.deltaTime;

        // 다이빙
        entity.isDiving = isSprinting;
        float rate = entity.dropSpeed;
        if (entity.isDiving)
        {
            rate = GameConfig.START_ALTITUDE / GameConfig.DROP_TIME_FAST;
        }

        entity.altitude -= rate * dt;

        // 공중 이동
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector2 airMove = moveInput * GameConfig.PLAYER_SPEED_AIR * dt;
            rb.MovePosition(rb.position + airMove);
        }

        // 맵 경계 클램프
        ClampToMap();

        // 착지
        if (entity.altitude <= 0f)
        {
            entity.altitude = 0f;
            entity.isDiving = false;
            // 착지 크레이터
            ParticleSpawner.Spawn(transform.position, Color.grey, 30);
        }
    }

    // ═══════════════════════════════════════════
    //  지상 전투
    // ═══════════════════════════════════════════

    private void UpdateGround()
    {
        float dt = Time.deltaTime;

        // ── 스테미나 ──
        float speed = GameConfig.PLAYER_SPEED_GROUND;
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (entity.fatigued)
        {
            entity.stamina += GameConfig.STAMINA_FATIGUE_RECOVER * dt;
            if (entity.stamina >= 100f)
            {
                entity.stamina = 100f;
                entity.fatigued = false;
            }
        }
        else
        {
            if (isSprinting && isMoving && entity.stamina > 0f)
            {
                speed *= GameConfig.SPRINT_MULTIPLIER;
                entity.stamina -= GameConfig.STAMINA_DRAIN * dt;
                if (entity.stamina <= 0f)
                {
                    entity.stamina = 0f;
                    entity.fatigued = true;
                }
            }
            else if (entity.stamina < 100f)
            {
                entity.stamina += GameConfig.STAMINA_RECOVER * dt;
                if (entity.stamina > 100f) entity.stamina = 100f;
            }
        }

        UIManager.Instance?.UpdateStaminaBar(entity.stamina, entity.fatigued);

        // ── 이동 (Rigidbody2D) ──
        if (isMoving)
        {
            Vector2 targetPos = rb.position + moveInput * speed * dt;
            rb.MovePosition(targetPos);
        }

        ClampToMap();

        // ── 조준 ──
        Vector2 dir = aimWorldPos - (Vector2)transform.position;
        entity.aimAngle = Mathf.Atan2(dir.y, dir.x);

        // 시각적 회전
        float angleDeg = entity.aimAngle * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        // ── 공격 ──
        if (isAttacking)
        {
            entity.Attack();
        }

        // ── 자기장 내부 여부에 따른 화면 효과 ──
        if (GameManager.Instance.currentState == GameState.Ground && GameManager.Instance.blueZone != null)
        {
            bool inZone = GameManager.Instance.blueZone.IsInsideSafeZone(transform.position);
            UIManager.Instance?.SetDamageOverlay(!inZone);
        }
    }

    private void ClampToMap()
    {
        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, 0f, GameConfig.MAP_SIZE);
        pos.y = Mathf.Clamp(pos.y, 0f, GameConfig.MAP_SIZE);
        rb.position = pos;
    }
}
