using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boss-arena end-of-run controller. When the marked boss dies, shows a congrats panel
/// and offers hub / main menu — both wipe RunStateManager so the next run starts clean.
/// Player death is still handled by PlayerDeathHandler (wipe → hub).
///
/// Setup:
///   1. Add to an empty GameObject in boss arena.
///   2. Assign bossStats (the boss enemy's StatsManager) and optionally congratsPanel /
///      congratsPrefab (clone of DeathScreen layout is fine).
///   3. Wire Congrats UI buttons to OnReturnToHub / OnReturnToMainMenu.
/// </summary>
public class BossStageController : MonoBehaviour
{
    [Header("Boss")]
    [Tooltip("StatsManager on the boss enemy. Auto-finds the first EnemyBase with isBoss if empty.")]
    public StatsManager bossStats;

    [Header("Win UI")]
    [Tooltip("Optional in-scene panel shown on boss death.")]
    public GameObject congratsPanel;

    [Tooltip("Spawned when Congrats Panel is empty.")]
    public GameObject congratsPrefab;

    [Header("Scenes")]
    public string hubSceneName = "hub";
    public string mainMenuSceneName = "MainMenu";

    private bool bossDefeated;

    private void Start()
    {
        if (bossStats == null)
        {
            foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                if (!enemy.isBoss) continue;
                bossStats = enemy.GetComponent<StatsManager>();
                if (bossStats != null) break;
            }
        }

        if (bossStats == null)
        {
            Debug.LogWarning($"BossStageController on '{name}': no boss StatsManager found. " +
                             "Mark an EnemyBase with isBoss or assign bossStats.");
            return;
        }

        bossStats.OnDied += HandleBossDied;

        if (congratsPanel != null)
            congratsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (bossStats != null)
            bossStats.OnDied -= HandleBossDied;
    }

    private void HandleBossDied()
    {
        if (bossDefeated) return;
        bossDefeated = true;

        // Clear the run the moment the boss dies rather than on the button press, so quitting at
        // the congrats screen can't leave a resumable boss fight behind.
        WipeRunState();

        ShowCongrats();
    }

    private void ShowCongrats()
    {
        if (congratsPanel != null)
        {
            congratsPanel.SetActive(true);
            WireButtons(congratsPanel);
            return;
        }

        if (congratsPrefab == null)
        {
            Debug.LogWarning($"BossStageController on '{name}': boss died but no congrats UI " +
                             "is assigned — loading hub.");
            OnReturnToHub();
            return;
        }

        GameObject instance = Instantiate(congratsPrefab);
        WireButtons(instance);
    }

    private void WireButtons(GameObject root)
    {
        // Prefer a dedicated controller if present; otherwise bind common button names.
        CongratsScreenController dedicated = root.GetComponent<CongratsScreenController>();
        if (dedicated != null)
        {
            dedicated.Initialize(this);
            return;
        }

        foreach (UnityEngine.UI.Button button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            string n = button.gameObject.name.ToLowerInvariant();
            button.onClick.RemoveAllListeners();
            if (n.Contains("hub") || n.Contains("restart"))
                button.onClick.AddListener(OnReturnToHub);
            else if (n.Contains("menu"))
                button.onClick.AddListener(OnReturnToMainMenu);
            else
                button.onClick.AddListener(OnReturnToHub);
        }
    }

    public void OnReturnToHub()
    {
        WipeRunState();
        SceneManager.LoadScene(hubSceneName);
    }

    public void OnReturnToMainMenu()
    {
        WipeRunState();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void WipeRunState()
    {
        if (RunStateManager.Instance != null)
            RunStateManager.Instance.Clear();

        // The run is finished, won rather than lost, but either way there's nothing to resume.
        SaveSystem.DeleteRun();
    }
}
