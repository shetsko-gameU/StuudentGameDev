using UnityEngine;

/// <summary>
/// Attach this to any enemy alongside EnemyBase.
/// When the enemy dies it rolls the loot table and spawns pickup prefabs.
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

        if (lootTable.guaranteedDrops != null)
        {
            foreach (LootTableSO.GuaranteedLootEntry entry in lootTable.guaranteedDrops)
                SpawnEntry(entry.pickupPrefab, entry.count);
        }

        if (lootTable.randomDrops != null)
        {
            foreach (LootTableSO.RandomLootEntry entry in lootTable.randomDrops)
            {
                if (Random.value <= entry.dropChance)
                    SpawnEntry(entry.pickupPrefab, entry.count);
            }
        }
    }

    private void SpawnEntry(GameObject pickupPrefab, int count)
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning($"LootDropper on '{name}': A loot entry has no prefab assigned.");
            return;
        }

        // Validate the prefab has a ModifierPickup on it before spawning
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
}
