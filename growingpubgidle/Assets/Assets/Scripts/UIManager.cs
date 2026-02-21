using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI 매니저. JS HTML UI를 Unity UGUI로 변환.
/// HP/스테미나 바, 킬카운터, 리더보드, 페이즈 텍스트 등.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD - Top")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text aliveCounterText;
    [SerializeField] private TMP_Text killCounterText;

    [Header("HUD - Right")]
    [SerializeField] private TMP_Text altitudeText;
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text lootCountText;

    [Header("HP & Stamina")]
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private Color hpNormalColor = Color.green;
    [SerializeField] private Color hpLowColor = Color.red;
    [SerializeField] private Color staminaNormalColor = new Color(1f, 0.84f, 0f); // yellow
    [SerializeField] private Color staminaFatigueColor = Color.gray;

    [Header("Panels")]
    [SerializeField] private GameObject centerPanel;        // 메뉴/결과 패널
    [SerializeField] private TMP_Text panelTitleText;
    [SerializeField] private TMP_Text panelDescText;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startButtonText;

    [Header("Damage Overlay")]
    [SerializeField] private Image damageOverlay;

    [Header("Leaderboard")]
    [SerializeField] private Transform leaderboardContent;
    [SerializeField] private GameObject leaderboardRowPrefab;

    [Header("Log")]
    [SerializeField] private TMP_Text logText;
    private Queue<string> logQueue = new Queue<string>();
    private const int MAX_LOG_LINES = 5;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 시작 버튼 이벤트
        if (startButton != null)
        {
            startButton.onClick.AddListener(() =>
            {
                GameManager.Instance?.StartGame();
            });
        }

        // 메뉴 표시
        ShowMenu();
    }

    // ═══════════════════════════════════════════
    //  상태 전환 UI
    // ═══════════════════════════════════════════

    public void ShowMenu()
    {
        if (centerPanel != null) centerPanel.SetActive(true);
        if (panelTitleText != null) panelTitleText.text = "GROWING PUBG IDLE";
        if (panelDescText != null)
        {
            panelDescText.text =
                "WASD=Move / Mouse=Aim / Click=Attack\n" +
                "Shift=Sprint\n" +
                "Loot <b>7 boxes</b> then escape via <b>green portal</b>!\n" +
                "WARNING: Blue zone deals 90 dmg/sec!";
        }
        if (startButtonText != null) startButtonText.text = "START GAME";
    }

    public void OnGameStart()
    {
        if (centerPanel != null) centerPanel.SetActive(false);
        if (phaseText != null)
        {
            phaseText.text = "AIR DROP!";
            phaseText.color = new Color(1f, 0.84f, 0f); // yellow
        }
        if (damageOverlay != null) SetDamageOverlay(false);

        // 로그 초기화
        logQueue.Clear();
        UpdateLogDisplay();
    }

    public void OnGroundPhase()
    {
        if (phaseText != null)
        {
            phaseText.text = "COMBAT!";
            phaseText.color = new Color(1f, 0.3f, 0.3f); // red
        }
    }

    public void OnGameEnd(bool win, bool allDead, EntityBase winner)
    {
        if (centerPanel != null) centerPanel.SetActive(true);
        if (startButtonText != null) startButtonText.text = "PLAY AGAIN";

        if (winner != null && win)
        {
            string winnerName = winner.isPlayer ? "You" : $"BOT {winner.entityId}";
            if (panelTitleText != null)
            {
                panelTitleText.text = "PORTAL ESCAPE!";
                panelTitleText.color = new Color(0.3f, 1f, 0.3f);
            }
            if (panelDescText != null)
                panelDescText.text = $"{winnerName} escaped through the portal!\nPerfect Victory!";
        }
        else if (!win)
        {
            EntityBase player = GameManager.Instance?.GetPlayer();
            if (panelTitleText != null)
            {
                panelTitleText.text = "GAME OVER";
                panelTitleText.color = Color.red;
            }
            if (panelDescText != null)
                panelDescText.text = $"You died.\nKills: {(player != null ? player.kills : 0)}";
        }
        else
        {
            EntityBase player = GameManager.Instance?.GetPlayer();
            if (panelTitleText != null)
            {
                panelTitleText.text = allDead ? "WINNER WINNER!" : "TIME OVER";
                panelTitleText.color = allDead ? new Color(1f, 0.84f, 0f) : new Color(0.3f, 0.6f, 1f);
            }
            if (panelDescText != null)
            {
                int kills = player != null ? player.kills : 0;
                panelDescText.text = allDead ? $"Last one standing! Kills: {kills}" : $"Survived! Kills: {kills}";
            }
        }
    }

    // ═══════════════════════════════════════════
    //  HUD 업데이트
    // ═══════════════════════════════════════════

    public void UpdateHUD(float gameTime, int aliveCount, int kills, float altitude, WeaponType weapon, int ammo, int lootCount)
    {
        if (timerText != null)
            timerText.text = Mathf.CeilToInt(gameTime).ToString();

        if (aliveCounterText != null)
            aliveCounterText.text = $"Alive: {aliveCount}";

        if (killCounterText != null)
            killCounterText.text = $"Kills: {kills}";

        if (altitudeText != null)
            altitudeText.text = $"Alt: {Mathf.FloorToInt(altitude)}pt";

        if (weaponText != null)
        {
            string wName = WeaponData.Get(weapon).displayName;
            if (weapon == WeaponType.Gun) wName += $" ({ammo})";
            weaponText.text = $"Weapon: {wName}";
        }

        if (lootCountText != null)
        {
            lootCountText.text = $"Loot: {lootCount} / {GameConfig.WIN_LOOT_COUNT}";
            if (lootCount >= GameConfig.WIN_LOOT_COUNT)
            {
                lootCountText.color = Color.green;
            }
        }
    }

    public void UpdateHPBar(float hp, float maxHp)
    {
        if (hpBarFill == null) return;
        float pct = hp / maxHp;
        hpBarFill.fillAmount = pct;
        hpBarFill.color = pct < 0.3f ? hpLowColor : hpNormalColor;
    }

    public void UpdateStaminaBar(float stamina, bool fatigued)
    {
        if (staminaBarFill == null) return;
        staminaBarFill.fillAmount = stamina / 100f;
        staminaBarFill.color = fatigued ? staminaFatigueColor : staminaNormalColor;
    }

    public void SetDamageOverlay(bool visible)
    {
        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = visible ? 0.3f : 0f;
            damageOverlay.color = c;
        }
    }

    // ═══════════════════════════════════════════
    //  리더보드
    // ═══════════════════════════════════════════

    public void UpdateLeaderboard(EntityBase[] entities)
    {
        if (leaderboardContent == null || leaderboardRowPrefab == null) return;

        // 기존 행 제거
        foreach (Transform child in leaderboardContent) Destroy(child.gameObject);

        // 정렬: 생존자 우선, 킬 순
        System.Array.Sort(entities, (a, b) =>
        {
            if (a.alive && !b.alive) return -1;
            if (!a.alive && b.alive) return 1;
            return b.kills.CompareTo(a.kills);
        });

        for (int i = 0; i < entities.Length; i++)
        {
            var e = entities[i];
            GameObject row = Instantiate(leaderboardRowPrefab, leaderboardContent);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = $"{i + 1}. {e.displayName}";
                texts[1].text = e.kills.ToString();

                Color textColor = e.alive
                    ? (e.isPlayer ? new Color(1f, 0.92f, 0.3f) : Color.white)
                    : Color.gray;
                texts[0].color = textColor;
                texts[1].color = textColor;
            }
        }
    }

    // ═══════════════════════════════════════════
    //  로그
    // ═══════════════════════════════════════════

    public void AddLog(string message)
    {
        logQueue.Enqueue(message);
        while (logQueue.Count > MAX_LOG_LINES) logQueue.Dequeue();
        UpdateLogDisplay();
        Debug.Log($"[Game] {message}");
    }

    private void UpdateLogDisplay()
    {
        if (logText == null) return;
        logText.text = string.Join("\n", logQueue.ToArray());
    }
}
