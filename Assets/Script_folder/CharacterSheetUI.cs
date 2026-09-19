using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Tab overlay that shows live StatsManager numbers and eaten PassiveManager lists.
/// Pauses like PauseMenu (timeScale 0) and yields to Escape so only one overlay is up.
/// </summary>
public class CharacterSheetUI : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    public StatsManager stats;
    public PassiveManager passives;
    public PlayerMove playerMove;
    public ComboRunner comboRunner;
    public AbilityRunner abilityRunner;
    public PauseMenu pauseMenu;

    [Header("UI (built automatically if missing)")]
    public GameObject sheetPanel;
    public TextMeshProUGUI statsColumn;
    public TextMeshProUGUI passivesColumn;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        EnsurePanel();
        Hide(restoreTime: false);
    }

    private void OnDisable()
    {
        if (IsOpen)
            Hide(restoreTime: pauseMenu == null || !pauseMenu.IsPaused);
    }

    private void Update()
    {
        if (stats != null && stats.IsDead) return;

        // Same low-level Keyboard read as PauseMenu + Escape so Tab still works at timeScale 0.
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (IsOpen) Hide();
        else Show();
    }

    public void Show()
    {
        if (IsOpen) return;
        if (stats != null && stats.IsDead) return;
        if (pauseMenu != null && pauseMenu.IsPaused) return;

        IsOpen = true;
        Time.timeScale = 0f;
        SetPlayerInputEnabled(false);
        Refresh();

        if (sheetPanel != null)
            sheetPanel.SetActive(true);
    }

    public void Hide()
    {
        Hide(restoreTime: pauseMenu == null || !pauseMenu.IsPaused);
    }

    public void Hide(bool restoreTime)
    {
        IsOpen = false;

        if (sheetPanel != null)
            sheetPanel.SetActive(false);

        if (restoreTime)
        {
            Time.timeScale = 1f;
            SetPlayerInputEnabled(true);
        }
    }

    public void Refresh()
    {
        if (statsColumn != null)
            statsColumn.text = BuildStatsText();
        if (passivesColumn != null)
            passivesColumn.text = BuildPassivesText();
    }

    private string BuildStatsText()
    {
        if (stats == null)
            return "STATS\n\n(no StatsManager)";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("STATS");
        sb.AppendLine();
        sb.AppendLine($"Health        {stats.CurrentHealth:0.#} / {stats.MaxHealth:0.#}");
        sb.AppendLine($"Attack        {stats.Attack:0.#}");
        sb.AppendLine($"Defense       {stats.Defense:0.#}");
        sb.AppendLine($"Move Speed    {stats.MoveSpeed:0.#}");
        sb.AppendLine($"Attack Speed  {stats.AttackSpeed:0.##}");
        sb.AppendLine($"Dodge Chance  {stats.DodgeChance:0.#}%");
        sb.AppendLine($"Health Steal  {stats.HealthSteal:0.#}");
        return sb.ToString();
    }

    private string BuildPassivesText()
    {
        if (passives == null)
            return "EATEN PASSIVES\n\n(no PassiveManager)";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("EATEN PASSIVES");
        sb.AppendLine();
        AppendList(sb, "Food", passives.DebugFoodPassives);
        AppendList(sb, "Stat Boosts", passives.DebugStatBoosts);
        AppendList(sb, "On Kill", passives.DebugKillPassives);
        AppendList(sb, "Debuffs", passives.DebugDebuffPassives);
        sb.AppendLine("Ult");
        sb.AppendLine(string.IsNullOrEmpty(passives.DebugUltAbility) ? "  (none)" : "  " + passives.DebugUltAbility);
        return sb.ToString();
    }

    private static void AppendList(StringBuilder sb, string title, IReadOnlyList<string> lines)
    {
        sb.AppendLine(title);
        if (lines == null || lines.Count == 0)
        {
            sb.AppendLine("  (none)");
            sb.AppendLine();
            return;
        }

        for (int i = 0; i < lines.Count; i++)
            sb.AppendLine("  " + lines[i]);
        sb.AppendLine();
    }

    private void SetPlayerInputEnabled(bool value)
    {
        if (value && stats != null && stats.IsDead) return;
        if (playerMove != null) playerMove.enabled = value;
        if (comboRunner != null) comboRunner.enabled = value;
        if (abilityRunner != null) abilityRunner.enabled = value;
    }

    private void ResolveReferences()
    {
        if (pauseMenu == null)
            pauseMenu = GetComponent<PauseMenu>() ?? Object.FindFirstObjectByType<PauseMenu>();

        if (stats != null && passives != null && playerMove != null && comboRunner != null && abilityRunner != null)
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        if (stats == null) stats = player.GetComponent<StatsManager>();
        if (passives == null) passives = player.GetComponent<PassiveManager>();
        if (playerMove == null) playerMove = player.GetComponent<PlayerMove>();
        if (comboRunner == null) comboRunner = player.GetComponent<ComboRunner>();
        if (abilityRunner == null) abilityRunner = player.GetComponent<AbilityRunner>();
    }

    private void EnsurePanel()
    {
        if (sheetPanel != null && statsColumn != null && passivesColumn != null)
            return;

        Transform existing = transform.Find("CharacterSheet");
        if (existing == null && transform.parent != null)
            existing = transform.parent.Find("CharacterSheet");

        if (existing != null)
        {
            sheetPanel = existing.gameObject;
            if (statsColumn == null)
            {
                Transform statsT = existing.Find("Card/StatsColumn");
                if (statsT != null) statsColumn = statsT.GetComponent<TextMeshProUGUI>();
            }
            if (passivesColumn == null)
            {
                Transform passivesT = existing.Find("Card/PassivesColumn");
                if (passivesT != null) passivesColumn = passivesT.GetComponent<TextMeshProUGUI>();
            }
            if (statsColumn != null && passivesColumn != null)
                return;
        }

        Transform canvas = GetComponentInParent<Canvas>() != null
            ? GetComponentInParent<Canvas>().transform
            : transform;

        GameObject panel = existing != null ? existing.gameObject : new GameObject("CharacterSheet", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas, false);
        sheetPanel = panel;

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        Image dim = panel.GetComponent<Image>();
        if (dim == null) dim = panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.raycastTarget = true;

        Transform cardT = panel.transform.Find("Card");
        GameObject card = cardT != null ? cardT.gameObject : new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(panel.transform, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(920f, 560f);
        cardRt.anchoredPosition = Vector2.zero;
        Image cardImg = card.GetComponent<Image>();
        if (cardImg == null) cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.08f, 0.1f, 0.94f);

        EnsureTmp(card.transform, "Title", "CHARACTER", 36f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -16f), new Vector2(0f, 56f));

        statsColumn = EnsureTmp(card.transform, "StatsColumn", "", 22f, FontStyles.Normal, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(28f, 28f), new Vector2(-12f, -72f));

        passivesColumn = EnsureTmp(card.transform, "PassivesColumn", "", 22f, FontStyles.Normal, TextAlignmentOptions.TopLeft,
            new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(12f, 28f), new Vector2(-28f, -72f));

        EnsureTmp(card.transform, "Hint", "Tab / Esc", 18f, FontStyles.Italic, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 32f));
    }

    private static TextMeshProUGUI EnsureTmp(
        Transform parent,
        string name,
        string text,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions align,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }
}
