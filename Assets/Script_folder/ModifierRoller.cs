using UnityEngine;
using System.Collections.Generic;

public static class ModifierRoller
{
    public static RolledModifierInstance Roll(StatsModifierSO so)
    {
        var inst = new RolledModifierInstance
        {
            source = so,
            durationSeconds = so.durationSeconds,
            stacks = 1,
            values = new Dictionary<(StatType stat, ModifierMode mode), float>()
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