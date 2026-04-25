using System.Collections.Generic;

public class RolledModifierInstance
{
    public StatsModifierSO source;
    public float durationSeconds;
    public int stacks = 1;

    // Values from lines where canStack == true  (multiplied by stack count)
    public Dictionary<(StatType stat, ModifierMode mode), float> stackableValues
        = new Dictionary<(StatType stat, ModifierMode mode), float>();

    // Values from lines where canStack == false  (always applied once)
    public Dictionary<(StatType stat, ModifierMode mode), float> nonStackableValues
        = new Dictionary<(StatType stat, ModifierMode mode), float>();

    // Keep 'values' as a combined read-only view so any existing code that reads it still works
    public Dictionary<(StatType stat, ModifierMode mode), float> values
        = new Dictionary<(StatType stat, ModifierMode mode), float>();
}

