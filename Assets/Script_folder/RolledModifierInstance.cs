using System.Collections.Generic;

public class RolledModifierInstance
{
    public StatsModifierSO source;
    public float durationSeconds;
    public int stacks = 1;

    public Dictionary<(StatType stat, ModifierMode mode), float> values
        = new Dictionary<(StatType stat, ModifierMode mode), float>();
}
