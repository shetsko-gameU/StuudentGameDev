using UnityEngine;

[CreateAssetMenu(menuName = "Game/Loot/Loot Table")]
public class LootTableSO : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
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

    [Header("Always Drops")]
    [Tooltip("These items drop every single time the enemy dies.")]
    public LootEntry[] guaranteedDrops;

    [Header("Random Drops")]
    [Tooltip("Each entry is rolled independently. Multiple can drop at once.")]
    public LootEntry[] randomDrops;
}