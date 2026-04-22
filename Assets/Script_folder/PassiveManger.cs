using System.Collections.Generic;
using UnityEngine;

public class PassiveManager : MonoBehaviour
{
    public StatsManager stats;
    public List<OnHitPassiveSO> onHitPassives = new List<OnHitPassiveSO>();

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

    public void AddFoodPassive(OnHitPassiveSO passive)
    {
        if (passive == null)
        {
            return;
        }

        if (passive.preventDuplicates)
        {
            if (onHitPassives.Contains(passive))
            {
                return;
            }
        }

        onHitPassives.Add(passive);
    }

    private void HandleDamaged(float finalDamage)
    {
        if (stats == null)
        {
            return;
        }

        for (int i = 0; i < onHitPassives.Count; i++)
        {
            OnHitPassiveSO p = onHitPassives[i];
            if (p == null || p.buffTemplate == null)
            {
                continue;
            }

            RolledModifierInstance rolled = ModifierRoller.Roll(p.buffTemplate);
            rolled.durationSeconds = p.buffDurationSeconds;
            stats.AddRolledModifier(rolled);
        }
    }
}