using System.Collections.Generic;
using UnityEngine;

public class PassiveEffectRunner : MonoBehaviour
{
    [Header("References")]
    public StatsManager stats;

    [Header("Passives")]
    public List<PassiveEffectSO> passives = new List<PassiveEffectSO>();

    private void Awake()
    {
        if (stats == null)
        {
            stats = GetComponent<StatsManager>();
        }
    }

    private void Start()
    {
        for (int i = 0; i < passives.Count; i++)
        {
            PassiveEffectSO p = passives[i];
            if (p != null && p.applyOnStart)
            {
                ApplyPassive(p);
            }
        }
    }

    public void ApplyPassive(PassiveEffectSO passive)
    {
        if (passive == null)
        {
            return;
        }

        if (stats == null)
        {
            Debug.LogWarning("PassiveEffectRunner: Missing StatsManager.");
            return;
        }

        if (passive.modifierToApply == null)
        {
            Debug.LogWarning("PassiveEffectRunner: Passive has no modifierToApply.");
            return;
        }

        // Roll and add as a modifier instance using your existing system
        RolledModifierInstance rolled = ModifierRoller.Roll(passive.modifierToApply);
        stats.AddRolledModifier(rolled);
    }
}