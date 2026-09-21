using UnityEngine;

/// <summary>
/// An enemy's drop table, rolled by LootDropper on death. Three entry types: guaranteed
/// (always drops, no chance field), random (independent dropChance roll per entry), and
/// currency (spawns a CurrencyPickup prefab with a runtime-rolled amount between min/max).
///
/// Setup:
///   1. Create via Assets, Create, Game, Loot, Loot Table.
///   2. Fill in guaranteedDrops/randomDrops with prefabs that have a ModifierPickup
///      component, and currencyDrops with prefabs that have a CurrencyPickup component.
///   3. Drag this asset into LootDropper.lootTable on the enemy prefab.
/// </summary>
[CreateAssetMenu(menuName = "Game/Loot/Loot Table")]
public class LootTableSO : ScriptableObject
{
    [System.Serializable]
    public class GuaranteedLootEntry
    {
        [Tooltip("Drag your pickup prefab from the Project window here (not from the scene).")]
        public GameObject pickupPrefab;

        [Min(1)]
        [Tooltip("How many of this item to drop at once.")]
        public int count = 1;
    }

    [System.Serializable]
    public class RandomLootEntry
    {
        [Tooltip("Drag your pickup prefab from the Project window here (not from the scene).")]
        public GameObject pickupPrefab;

        [Range(0f, 1f)]
        [Tooltip("0 = never drops. 1 = always drops. 0.25 = 25% chance.")]
        public float dropChance = 0.5f;

        [Min(1)]
        [Tooltip("How many of this item to drop at once.")]
        public int count = 1;
    }

    [System.Serializable]
    public class CurrencyLootEntry
    {
        [Tooltip("Prefab with a CurrencyPickup component. This is the world object that spawns.")]
        public GameObject pickupPrefab;

        [Min(1)]
        [Tooltip("Minimum amount this pickup is worth when collected.")]
        public int minAmount = 1;

        [Min(1)]
        [Tooltip("Maximum amount this pickup is worth (inclusive). Set equal to min for a fixed value.")]
        public int maxAmount = 5;

        [Range(0f, 1f)]
        [Tooltip("Drop chance. 1 = always, 0 = never.")]
        public float dropChance = 1f;
    }

    [Header("Always Drops")]
    [Tooltip("These items drop every single time the enemy dies.")]
    public GuaranteedLootEntry[] guaranteedDrops;

    [Header("Random Drops")]
    [Tooltip("Each entry is rolled independently. Multiple can drop at once.")]
    public RandomLootEntry[] randomDrops;

    [Header("Currency Drops")]
    [Tooltip("Currency awarded directly to the player's CurrencyTracker when this enemy dies.")]
    public CurrencyLootEntry[] currencyDrops;
}