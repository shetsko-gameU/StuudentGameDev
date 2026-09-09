using System.Collections.Generic;

/// <summary>
/// The concrete, already-rolled result of applying a StatsModifierSO template — produced by
/// ModifierRoller.Roll, tracked by StatsManager.AddRolledModifier/RemoveRolledInstance, and
/// what PassiveManager stores per-entry so it can remove the exact values it granted (rather
/// than "all modifiers from this source") when a passive is upgraded or unequipped. Plain
/// runtime data, never an asset — nothing to set up in the Editor.
/// </summary>
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

