using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Static helper that turns a StatsModifierSO's min/max roll lines into a concrete
/// RolledModifierInstance — rolling each line's value, applying its rarity multiplier and
/// step rounding, and sorting stackable vs. non-stackable lines into separate buckets for
/// StatsManager. Called by PassiveManager, PlayerConsume, KillPassiveTrigger, and
/// DebuffOnHitTrigger anywhere a StatsModifierSO template needs to become a live instance.
///
/// Setup: nothing to configure — call ModifierRoller.Roll(yourStatsModifierSO) directly,
/// no instance or GameObject needed.
/// </summary>
public static class ModifierRoller
{
    public static RolledModifierInstance Roll(StatsModifierSO so)
    {
        var inst = new RolledModifierInstance
        {
            source = so,
            durationSeconds = so.durationSeconds,
            stacks = 1,
        };

        float rarityMult = GetRarityMultiplier(so.rarity);

        foreach (var line in so.lines)
        {
            float min = Mathf.Min(line.minValue, line.maxValue);
            float max = Mathf.Max(line.minValue, line.maxValue);

            float rolled = Random.Range(min, max) * rarityMult;

            if (line.step > 0f)
                rolled = Mathf.Round(rolled / line.step) * line.step;

            var key = (line.stat, line.mode);

            // Put the value in the correct bucket based on whether it can stack
            if (line.canStack)
            {
                if (inst.stackableValues.ContainsKey(key))
                    inst.stackableValues[key] += rolled;
                else
                    inst.stackableValues[key] = rolled;
            }
            else
            {
                if (inst.nonStackableValues.ContainsKey(key))
                    inst.nonStackableValues[key] += rolled;
                else
                    inst.nonStackableValues[key] = rolled;
            }

            // Keep the combined 'values' dict up to date (for any legacy reads)
            if (inst.values.ContainsKey(key))
                inst.values[key] += rolled;
            else
                inst.values[key] = rolled;
        }

        return inst;
    }

    private static float GetRarityMultiplier(Rarity r)
    {
        return r switch
        {
            Rarity.Common => 1.0f,
            Rarity.Rare => 1.15f,
            Rarity.Epic => 1.35f,
            Rarity.Legendary => 1.65f,
            _ => 1.0f
        };
    }
}