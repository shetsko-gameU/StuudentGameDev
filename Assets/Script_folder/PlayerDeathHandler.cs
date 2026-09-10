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

    [Tooltip("Optional. When present, death is applied through the state machine's Dead parameter " +
             "instead of the Death Animator Trigger below.")]
    public PlayerStateMachine stateMachine;

    [Header("Death Animation")]
    [Tooltip("Must match a Trigger parameter on the Animator Controller with a transition into a death state.")]
    public string deathAnimatorTrigger = "Death";

    [Tooltip("Seconds after triggering the death animation before the death screen appears. Tune to match the clip length.")]
    public float deathScreenDelay = 1.5f;

    [Header("Death Screen UI")]
    [Tooltip("Optional per-scene panel shown after the death animation. Hidden automatically on " +
             "Awake. Leave empty to use Death Screen Prefab instead, which is the normal setup.")]
    public GameObject deathScreenPanel;

    [Tooltip("Spawned on death when Death Screen Panel is empty. Points at Assets/UI/DeathScreen.prefab " +
             "by default, which wires its own buttons - so a new scene needs no death screen setup.")]
    public GameObject deathScreenPrefab;

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
        if (stateMachine == null) stateMachine = GetComponent<PlayerStateMachine>();

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

        // Prefer the state machine: it writes the Dead bool from the shared parameter
        // contract and skips the write when the controller does not declare it. The direct
        // trigger below is the fallback for a player prefab with no PlayerStateMachine, and
        // it fires blind - if the controller has no such trigger Unity logs a warning, which
        // is exactly what happens today on Player.controller.
        if (stateMachine != null)
            stateMachine.ApplyDeath();
        else if (animator != null && !string.IsNullOrEmpty(deathAnimatorTrigger))
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
        // A panel placed in the scene wins, so a bespoke death screen can still override the
        // shared one.
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
            return;
        }

        if (deathScreenPrefab == null)
        {
            Debug.LogWarning($"PlayerDeathHandler on '{name}': the player is dead but neither " +
                             "Death Screen Panel nor Death Screen Prefab is assigned, so there is " +
                             "no way to restart. Assign Assets/UI/DeathScreen.prefab.");
            return;
        }

        GameObject instance = Instantiate(deathScreenPrefab);

        // The prefab wires its own buttons; it only needs to know which handler to call.
        DeathScreenController controller = instance.GetComponent<DeathScreenController>();
        if (controller != null)
            controller.Initialize(this);
        else
            Debug.LogWarning($"PlayerDeathHandler on '{name}': Death Screen Prefab has no " +
                             "DeathScreenController, so its buttons are not connected to anything.");
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
