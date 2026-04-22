using UnityEngine;

public class GainStatOnHit : MonoBehaviour
{
    [Header("References")]
    public StatsManager stats;

    [Header("Buff Template")]
    public StatsModifierSO buffTemplate;

    [Tooltip("Overrides the template duration. Set 0 to use the template's durationSeconds.")]
    [Min(0f)] public float overrideDurationSeconds = 3f;

    private void Awake()
    {
        if (stats == null)
        {
            stats = GetComponent<StatsManager>();
        }
    }

    private void OnEnable()
    {
        if (stats != null)
        {
            stats.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.OnDamaged -= HandleDamaged;
        }
    }

    private void HandleDamaged(float finalDamage)
    {
        if (stats == null)
        {
            return;
        }

        if (buffTemplate == null)
        {
            Debug.LogWarning("GainStatOnHit: buffTemplate is null.");
            return;
        }

        // Roll from your existing SO system
        RolledModifierInstance rolled = ModifierRoller.Roll(buffTemplate);

        // Override duration if desired
        if (overrideDurationSeconds > 0f)
        {
            rolled.durationSeconds = overrideDurationSeconds;
        }

        stats.AddRolledModifier(rolled);
    }
}