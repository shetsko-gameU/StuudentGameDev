using UnityEngine;

/// <summary>
/// Add to the player alongside PassiveManager and ComboRunner.
///
/// Listens to AttackHitbox.OnEnemyHit — which only fires when a swing actually connects
/// and wasn't dodged — and, for every active DebuffOnHitPassiveSO, rolls a proc chance and
/// applies its debuffTemplate to the ENEMY that was hit (not the player).
///
/// To make a food grant a debuff-on-hit passive: create a DebuffOnHitPassiveSO asset and
/// link it through PlayerConsume's Food → Debuff Boost list (or
/// PassiveManager.startingDebuffPassives for testing).
/// </summary>
public class DebuffOnHitTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Auto-found if on the same GameObject. hitbox falls back to ComboRunner.hitbox, " +
             "then to a child search, since AttackHitbox usually lives on a child (the weapon).")]
    public ComboRunner comboRunner;
    public AttackHitbox hitbox;
    public PassiveManager passiveManager;

    // ------------------------------------------------------------------ Lifecycle

    private void Awake()
    {
        if (comboRunner == null) comboRunner = GetComponent<ComboRunner>();
        if (hitbox == null && comboRunner != null) hitbox = comboRunner.hitbox;
        if (hitbox == null) hitbox = GetComponentInChildren<AttackHitbox>();
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();

        if (hitbox == null) Debug.LogError($"DebuffOnHitTrigger on '{name}': AttackHitbox missing (checked ComboRunner.hitbox and children).");
        if (passiveManager == null) Debug.LogError($"DebuffOnHitTrigger on '{name}': PassiveManager missing.");
    }

    private void OnEnable()
    {
        if (hitbox != null)
            hitbox.OnEnemyHit += HandleEnemyHit;
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.OnEnemyHit -= HandleEnemyHit;
    }

    // ------------------------------------------------------------------ Handler

    private void HandleEnemyHit(StatsManager enemyHit)
    {
        if (passiveManager == null || enemyHit == null) return;

        foreach (DebuffOnHitPassiveSO passive in passiveManager.ActiveDebuffPassives())
        {
            if (passive == null || passive.debuffTemplate == null) continue;
            if (Random.value > passive.procChance) continue;

            Debug.Log($"DebuffOnHitTrigger: Proc'd '{passive.displayName}' on '{enemyHit.name}'");

            RolledModifierInstance roll = ModifierRoller.Roll(passive.debuffTemplate);
            enemyHit.AddRolledModifier(roll);

            if (passive.SpawnEntity != null)
                Instantiate(passive.SpawnEntity, enemyHit.transform.position, Quaternion.identity);
        }
    }
}
