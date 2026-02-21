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
        // 💡 1. 게임 상태 예외 처리 개선 (GameManager.IsPlayable 활용)
        if (!entity.alive || !GameManager.Instance.IsPlayable())
        {
            // 입력이 막히거나 조작할 수 없는 상태(Menu, Pause 등)일 때는 관성 제거
            rb.linearVelocity = Vector2.zero; 
            return;
        }

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
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ky += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ky -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) kx -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) kx += 1f;

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
        isAttacking = false;
        if (Input.touchCount == 0) 
        {
            // 마우스 환경 (에디터/PC): UI 클릭이 아닐 때만 발사 인정
            bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                                 UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButton(0) && !pointerOverUI)
            {
                isAttacking = true;
            }
        }

        // 모바일 발사 버튼 오버라이드
        if (mobileInput != null) isAttacking |= mobileInput.IsAttacking;

        // 조준 방향 (마우스 또는 터치)
        if (mainCam != null)
        {
            if (mobileInput != null && mobileInput.HasAimInput)
            {
                aimWorldPos = (Vector2)transform.position + mobileInput.AimDirection * 1f;
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

        // 💡 2. 공중 이동 (velocity로 델타타임 물리 충돌 해결)
        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = moveInput * GameConfig.PLAYER_SPEED_AIR;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // 💡 3. 맵 경계 클램프 (물리 처리와 겹치지 않게 transform 직접 조작)
        Vector2 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, 0f, GameConfig.MAP_SIZE);
        clampedPos.y = Mathf.Clamp(clampedPos.y, 0f, GameConfig.MAP_SIZE);
        transform.position = clampedPos;

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

        // 💡 4. 지상 이동 (velocity로 즉각적이고 안정적인 반응 구현)
        if (isMoving)
        {
            rb.linearVelocity = moveInput * speed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero; // 조이스틱에서 손을 떼면 즉시 정지
        }

        // 💡 5. 맵 경계 클램프
        Vector2 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, 0f, GameConfig.MAP_SIZE);
        clampedPos.y = Mathf.Clamp(clampedPos.y, 0f, GameConfig.MAP_SIZE);
        transform.position = clampedPos;

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
        if (GameManager.Instance.blueZone != null)
        {
            bool inZone = GameManager.Instance.blueZone.IsInsideSafeZone(transform.position);
            UIManager.Instance?.SetDamageOverlay(!inZone);
        }
    }
}