using UnityEngine;

/// <summary>
/// Attach this to any enemy alongside EnemyBase.
/// When the enemy dies it rolls the loot table and spawns pickup prefabs.
<<<<<<< HEAD
=======
/// Currency drops are awarded directly to the player's CurrencyTracker —
/// no prefab needed for them.
>>>>>>> ScriptBreanchfixs
/// </summary>
public class LootDropper : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The enemy's StatsManager. Auto-found if left empty.")]
    public StatsManager stats;

    [Header("Loot")]
    public LootTableSO lootTable;

    [Header("Drop Settings")]
    public float spawnHeightOffset = 0.5f;
    public float scatterRadius = 0.6f;

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<StatsManager>();

        if (stats == null)
        {
            Debug.LogError($"LootDropper on '{name}': No StatsManager found.");
            return;
        }

        stats.OnDied += HandleDied;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (lootTable == null) return;

<<<<<<< HEAD
        if (lootTable.guaranteedDrops != null)
        {
            foreach (LootTableSO.LootEntry entry in lootTable.guaranteedDrops)
                SpawnEntry(entry);
        }

        if (lootTable.randomDrops != null)
        {
            foreach (LootTableSO.LootEntry entry in lootTable.randomDrops)
            {
                if (Random.value <= entry.dropChance)
                    SpawnEntry(entry);
=======
        // Guaranteed item drops
        if (lootTable.guaranteedDrops != null)
        {
            foreach (LootTableSO.GuaranteedLootEntry entry in lootTable.guaranteedDrops)
                SpawnEntry(entry.pickupPrefab, entry.count);
        }

        // Random item drops
        if (lootTable.randomDrops != null)
        {
            foreach (LootTableSO.RandomLootEntry entry in lootTable.randomDrops)
            {
                if (Random.value <= entry.dropChance)
                    SpawnEntry(entry.pickupPrefab, entry.count);
            }
        }

        // Currency drops — spawned as world pickup prefabs with a scattered position
        if (lootTable.currencyDrops != null)
        {
            foreach (LootTableSO.CurrencyLootEntry entry in lootTable.currencyDrops)
            {
                if (entry.pickupPrefab == null)
                {
                    Debug.LogWarning($"LootDropper on '{name}': A currency loot entry has no prefab assigned.");
                    continue;
                }

                if (Random.value > entry.dropChance) continue;

                CurrencyPickup template = entry.pickupPrefab.GetComponent<CurrencyPickup>();
                if (template == null)
                {
                    Debug.LogWarning($"LootDropper on '{name}': Prefab '{entry.pickupPrefab.name}' " +
                                     "has no CurrencyPickup component — skipping.");
                    continue;
                }

                // Roll the amount and stamp it onto the spawned instance
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                Vector3 spawnPos = GetScatteredPosition();
                GameObject go = Instantiate(entry.pickupPrefab, spawnPos, Quaternion.identity);
                go.GetComponent<CurrencyPickup>().amount = amount;
>>>>>>> ScriptBreanchfixs
            }
        }
    }

<<<<<<< HEAD
    private void SpawnEntry(LootTableSO.LootEntry entry)
    {
        if (entry.pickupPrefab == null)
=======
    private void SpawnEntry(GameObject pickupPrefab, int count)
    {
        if (pickupPrefab == null)
>>>>>>> ScriptBreanchfixs
        {
            Debug.LogWarning($"LootDropper on '{name}': A loot entry has no prefab assigned.");
            return;
        }

        // Validate the prefab has a ModifierPickup on it before spawning
<<<<<<< HEAD
        if (entry.pickupPrefab.GetComponent<ModifierPickup>() == null)
        {
            Debug.LogWarning($"LootDropper on '{name}': Prefab '{entry.pickupPrefab.name}' " +
                             "has no ModifierPickup component � skipping.");
            return;
        }

        for (int i = 0; i < entry.count; i++)
        {
            Vector3 spawnPos = GetScatteredPosition();
            Instantiate(entry.pickupPrefab, spawnPos, Quaternion.identity);
=======
        if (pickupPrefab.GetComponent<ModifierPickup>() == null)
        {
            Debug.LogWarning($"LootDropper on '{name}': Prefab '{pickupPrefab.name}' " +
                             "has no ModifierPickup component — skipping.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetScatteredPosition();
            Instantiate(pickupPrefab, spawnPos, Quaternion.identity);
>>>>>>> ScriptBreanchfixs
        }
    }

    private Vector3 GetScatteredPosition()
    {
        Vector3 basePos = transform.position + Vector3.up * spawnHeightOffset;

        if (scatterRadius <= 0f)
            return basePos;

        Vector2 circle = Random.insideUnitCircle * scatterRadius;
        return basePos + new Vector3(circle.x, 0f, circle.y);
    }
<<<<<<< HEAD
}
=======

}
>>>>>>> ScriptBreanchfixs
