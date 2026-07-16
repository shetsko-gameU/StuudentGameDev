using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Add to the player alongside StatsManager. Subscribes to StatsManager.OnDied.
///
/// On death: freezes movement, cancels any in-flight attack, and disables
/// move/attack/ability/passives so none of them can trigger again — then
/// plays the death animation and shows the death screen after a delay.
///
/// Wire the death screen's three buttons' OnClick() to this component's
/// OnRestartRun / OnReturnToMainMenu / OnReturnToHub methods.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    public StatsManager stats;
    public PlayerMove playerMove;
    public ComboRunner comboRunner;
    public AbilityRunner abilityRunner;
    public PassiveManager passiveManager;
    public ComboPassiveTrigger comboPassiveTrigger;
    public KillPassiveTrigger killPassiveTrigger;
    public Animator animator;

    [Header("Death Animation")]
    [Tooltip("Must match a Trigger parameter on the Animator Controller with a transition into a death state.")]
    public string deathAnimatorTrigger = "Death";

    [Tooltip("Seconds after triggering the death animation before the death screen appears. Tune to match the clip length.")]
    public float deathScreenDelay = 1.5f;

    [Header("Death Screen UI")]
    [Tooltip("Panel shown after the death animation. Hidden automatically on Awake.")]
    public GameObject deathScreenPanel;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Scene to load for \"Return to Hub\". Must exist and be added to Build Settings.")]
    public string hubSceneName = "Hub";

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (stats == null) stats = GetComponent<StatsManager>();
        if (playerMove == null) playerMove = GetComponent<PlayerMove>();
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (abilityRunner == null) abilityRunner = GetComponent<AbilityRunner>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (comboPassiveTrigger == null) comboPassiveTrigger = GetComponent<ComboPassiveTrigger>();
        if (killPassiveTrigger == null) killPassiveTrigger = GetComponent<KillPassiveTrigger>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (stats == null)
            Debug.LogError($"PlayerDeathHandler on '{name}': No StatsManager found.");

        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnDied -= HandleDeath;
    }

    // ------------------------------------------------------------------ Death

    private void HandleDeath()
    {
        DisablePlayerSystems();

        if (animator != null && !string.IsNullOrEmpty(deathAnimatorTrigger))
            animator.SetTrigger(deathAnimatorTrigger);

        Invoke(nameof(ShowDeathScreen), deathScreenDelay);
    }

    private void DisablePlayerSystems()
    {
        // Movement — stop the agent too. DashAbilitySO's coroutine checks IsDead itself
        // and bails out, but stopping the agent here covers any other in-flight movement.
        // isStopped throws on a disabled/off-mesh agent (it's disabled mid-fall) — in that
        // case leave physics alone so a falling corpse just keeps falling, which reads fine.
        if (playerMove != null)
        {
            if (playerMove.agent != null && playerMove.agent.enabled && playerMove.agent.isOnNavMesh)
            {
                playerMove.agent.isStopped = true;
                playerMove.agent.velocity = Vector3.zero;
            }
            playerMove.enabled = false;
        }

        // Attack — cancel any in-flight ResolveHit coroutine and force the hitbox off,
        // so a swing already in motion can't still land a hit after death.
        if (comboRunner != null)
        {
            comboRunner.StopAllCoroutines();
            comboRunner.enabled = false;
            if (comboRunner.hitbox != null)
                comboRunner.hitbox.SetActive(false);
        }

        // Ability
        if (abilityRunner != null)
            abilityRunner.enabled = false;

        // Passives — OnDisable() on these unsubscribes them from StatsManager.OnDamaged,
        // ComboRunner's combo events, and the static OnAnyDied kill event, so nothing fires again.
        if (passiveManager != null) passiveManager.enabled = false;
        if (comboPassiveTrigger != null) comboPassiveTrigger.enabled = false;
        if (killPassiveTrigger != null) killPassiveTrigger.enabled = false;
    }

    private void ShowDeathScreen()
    {
        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(true);
    }

    // ------------------------------------------------------------------ Death screen buttons

    public void OnRestartRun()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnReturnToHub()
    {
        SceneManager.LoadScene(hubSceneName);
    }
}
