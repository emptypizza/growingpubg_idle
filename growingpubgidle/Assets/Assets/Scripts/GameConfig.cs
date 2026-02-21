using UnityEngine;

/// <summary>
/// 게임 전역 상수 및 무기 데이터.
/// JS의 CONSTANTS, WEAPONS 객체를 C# static class로 변환.
/// </summary>
public static class GameConfig
{
    // ── 맵 ──
    public const float MAP_SIZE = 300f; // Unity 단위 (웹 3000px → 300 units)
    public const float UNIT_SCALE = 0.1f; // 웹 px → Unity 단위 변환 배율

    // ── 강하 ──
    public const float START_ALTITUDE = 100f;
    public const float DROP_TIME_NORMAL = 10f;
    public const float DROP_TIME_FAST = 5f;
    public const float PLAYER_SPEED_AIR = 30f;

    // ── 지상 ──
    public const float PLAYER_SPEED_GROUND = 20f;
    public const float SPRINT_MULTIPLIER = 1.5f;
    public const float STAMINA_DRAIN = 40f;
    public const float STAMINA_RECOVER = 20f;
    public const float STAMINA_FATIGUE_RECOVER = 33.3f;
    public const float PLAYER_RADIUS = 1.5f;

    // ── 전투 ──
    public const float PLAYER_MAX_HP = 400f;
    public const float BOT_MAX_HP = 100f;
    public const float BLUEZONE_DAMAGE = 90f; // 초당
    public const float CRATER_RADIUS = 6f;

    // ── 보급 / 포탈 ──
    public const float LOOT_SPAWN_INTERVAL = 5f;
    public const int WIN_LOOT_COUNT = 7;
    public const float PORTAL_SPAWN_TIME = 20f;
    public const float PORTAL_DURATION = 10f;
    public const float PORTAL_RADIUS = 5f;

    // ── 타이머 ──
    public const float GROUND_TIME = 60f;

    // ── 엔티티 수 ──
    public const int TOTAL_ENTITIES = 10; // 1 player + 9 bots
    public const int INITIAL_LOOT_COUNT = 30;
    public const int MAP_OBJECT_COUNT = 60;
}

/// <summary>
/// 게임 상태 열거형
/// </summary>
public enum GameState
{
    Menu,
    Dropping,
    Ground,
    End
}

/// <summary>
/// 무기 종류
/// </summary>
public enum WeaponType
{
    None,
    Knife,
    Gun,
    Shield
}

/// <summary>
/// 무기 데이터 구조체
/// </summary>
[System.Serializable]
public struct WeaponData
{
    public string displayName;
    public float range;
    public float damage;
    public float delay; // 초 단위
    public float bulletSpeed; // Gun 전용
    public float defense;     // Shield 전용

    public static WeaponData Get(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Knife:
                return new WeaponData
                {
                    displayName = "Knife",
                    range = 6f,
                    damage = 40f,
                    delay = 0.4f
                };
            case WeaponType.Gun:
                return new WeaponData
                {
                    displayName = "Rifle",
                    range = 50f,
                    damage = 15f,
                    delay = 0.15f,
                    bulletSpeed = 80f
                };
            case WeaponType.Shield:
                return new WeaponData
                {
                    displayName = "Shield",
                    range = 4f,
                    damage = 10f,
                    delay = 0.6f,
                    defense = 0.5f
                };
            default: // None = Fist
                return new WeaponData
                {
                    displayName = "Fist",
                    range = 4f,
                    damage = 5f,
                    delay = 0.5f
                };
        }
    }
}
