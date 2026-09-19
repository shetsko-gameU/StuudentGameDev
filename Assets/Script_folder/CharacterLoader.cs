using UnityEngine;

/// <summary>
/// Puts a chosen CharacterSO's body onto the Player. Only the mesh child, its animator and the
/// attack data change — the Player GameObject itself is never replaced, because every scene, the
/// camera, the UI and the enemies hold references to it.
///
/// [DefaultExecutionOrder(-100)] so the swap happens in Awake before the components that resolve
/// their Animator with GetComponentInChildren, which means a spawned player is already wearing
/// the right body by the time anything binds to it.
///
/// Setup: one per Player prefab. Assign meshRoot (the 'mesh' child) and set authoredCharacter to
/// the body the prefab ships with, so no swap work is done when that's the chosen one.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CharacterLoader : MonoBehaviour
{
    /// <summary>PlayerPrefs key for the remembered choice, alongside SettingsMenu's Settings_* keys.</summary>
    public const string PrefsKey = "Profile_CharacterId";

    [Header("Setup")]
    [Tooltip("The mesh child this component swaps. Auto-found by name ('mesh') if empty.")]
    public Transform meshRoot;

    [Tooltip("The character this prefab is already built as. Choosing it costs no swap work.")]
    public CharacterSO authoredCharacter;

    [Header("Rebinding")]
    [Tooltip("Components whose Animator reference is re-pointed at the new mesh. Auto-found.")]
    public PlayerMove playerMove;
    public PlayerStateMachine stateMachine;
    public ComboRunner comboRunner;
    public PlayerDeathHandler deathHandler;
    public AttackHitbox attackHitbox;

    public CharacterSO Current { get; private set; }
    public string CurrentCharacterId => Current != null ? Current.id : null;

    /// <summary>
    /// The remembered character, independent of any run. Kept in PlayerPrefs so it survives death
    /// and a deleted save — losing a run shouldn't change who you're playing as.
    /// </summary>
    public static string SavedCharacterId
    {
        get { return PlayerPrefs.GetString(PrefsKey, string.Empty); }
        set
        {
            PlayerPrefs.SetString(PrefsKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        if (meshRoot == null) meshRoot = transform.Find("mesh");
        if (playerMove == null) playerMove = GetComponent<PlayerMove>();
        if (stateMachine == null) stateMachine = GetComponent<PlayerStateMachine>();
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (deathHandler == null) deathHandler = GetComponent<PlayerDeathHandler>();
        if (attackHitbox == null) attackHitbox = GetComponentInChildren<AttackHitbox>(true);

        Current = authoredCharacter;

        // A run in progress wins over the remembered choice: a resumed save must hand back the
        // body that run was started with, even if the player has since picked someone else.
        string runId = RunStateManager.Instance.HasData ? RunStateManager.Instance.CharacterId : null;
        string wanted = !string.IsNullOrEmpty(runId) ? runId : SavedCharacterId;

        if (string.IsNullOrEmpty(wanted)) return;

        if (authoredCharacter != null && authoredCharacter.id == wanted) return;

        CharacterSO character = CharacterLibrary.Find(wanted);
        if (character == null)
        {
            Debug.LogWarning($"CharacterLoader on '{name}': character id '{wanted}' isn't in the " +
                             "CharacterLibrary — keeping the prefab's own body.");
            return;
        }

        Apply(character);
    }

    /// <summary>
    /// Swaps in a character's body. Safe to call mid-game (the hub swap station does); the new
    /// mesh lands with the same offset and scale the prefab authored, and everything holding an
    /// Animator reference is re-pointed at it.
    /// </summary>
    public void Apply(CharacterSO character)
    {
        if (character == null) return;

        if (character.meshPrefab == null)
        {
            Debug.LogError($"CharacterLoader on '{name}': character '{character.name}' has no meshPrefab.");
            return;
        }

        Animator oldAnimator = meshRoot != null ? meshRoot.GetComponent<Animator>() : null;
        Transform oldMesh = meshRoot;

        // Read the old Animator's setup before it goes away, so the new body can inherit it.
        bool hadAnimator = oldAnimator != null;
        bool applyRootMotion = hadAnimator && oldAnimator.applyRootMotion;
        AnimatorCullingMode cullingMode = hadAnimator ? oldAnimator.cullingMode : default(AnimatorCullingMode);
        AnimatorUpdateMode updateMode = hadAnimator ? oldAnimator.updateMode : default(AnimatorUpdateMode);

        if (oldMesh != null)
        {
            // Unparent before destroying: Destroy is deferred to end of frame, and a still-attached
            // child would keep turning up in GetComponentInChildren<Animator>() until then.
            oldMesh.SetParent(null, false);
            Destroy(oldMesh.gameObject);
        }

        GameObject newMesh = Instantiate(character.meshPrefab, transform);
        newMesh.name = "mesh";
        newMesh.transform.SetSiblingIndex(0);
        newMesh.transform.localPosition = new Vector3(0f, character.meshLocalY, 0f);
        newMesh.transform.localRotation = Quaternion.identity;
        newMesh.transform.localScale = Vector3.one * character.meshScale;

        meshRoot = newMesh.transform;

        Animator animator = newMesh.GetComponent<Animator>();
        if (animator == null) animator = newMesh.AddComponent<Animator>();

        animator.runtimeAnimatorController = character.animatorController;

        // Carry over the Animator setup from the body being replaced rather than hardcoding it, so
        // a swapped character behaves exactly like the authored prefab did — in particular root
        // motion stays off, since PlayerMove owns the transform.
        if (hadAnimator)
        {
            animator.applyRootMotion = applyRootMotion;
            animator.cullingMode = cullingMode;
            animator.updateMode = updateMode;
        }

        RebindAnimator(animator, oldAnimator, oldMesh);

        if (character.combo != null && comboRunner != null)
            comboRunner.combo = character.combo;

        if (attackHitbox != null)
        {
            attackHitbox.isRanged = character.isRanged;
            attackHitbox.projectilePrefab = character.projectilePrefab;
            attackHitbox.projectileSpeed = character.projectileSpeed;
        }

        Current = character;
    }

    private void RebindAnimator(Animator animator, Animator oldAnimator, Transform oldMesh)
    {
        // Only repoint references that were unset or pointing at the body just removed — anything
        // aimed somewhere else was deliberate. PlayerMove.playerModel, for instance, is the Player
        // root on both prefabs, not the mesh.
        if (playerMove != null)
        {
            if (playerMove.animator == null || playerMove.animator == oldAnimator)
                playerMove.animator = animator;

            if (playerMove.playerModel == null || playerMove.playerModel == oldMesh)
                playerMove.playerModel = animator.transform;
        }

        if (stateMachine != null && (stateMachine.animator == null || stateMachine.animator == oldAnimator))
            stateMachine.RebindAnimator(animator);

        if (comboRunner != null && (comboRunner.animator == null || comboRunner.animator == oldAnimator))
            comboRunner.animator = animator;

        if (deathHandler != null && (deathHandler.animator == null || deathHandler.animator == oldAnimator))
            deathHandler.animator = animator;
    }
}
