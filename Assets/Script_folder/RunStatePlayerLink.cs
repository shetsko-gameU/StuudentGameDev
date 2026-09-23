using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add to the Player prefab. Bridges the Player's runtime systems (PassiveManager, Inventory,
/// CurrencyTracker) to RunStateManager so state survives a mid-run scene load — e.g. walking
/// through RoomExit's portal.
///
/// [DefaultExecutionOrder(100)] forces this component's Start() to run after every other
/// component's own Start() (PassiveManager applying its scene-default passives, Inventory /
/// CurrencyTracker initializing), so a restored snapshot always lands on top of a
/// fully-initialized Player instead of racing its own setup.
/// </summary>
[DefaultExecutionOrder(100)]
public class RunStatePlayerLink : MonoBehaviour
{
    [Header("References")]
    public PassiveManager passiveManager;
    public Inventory inventory;
    public CurrencyTracker currencyTracker;
    public StatsManager stats;
    public CharacterLoader characterLoader;

    private void Awake()
    {
        if (passiveManager == null) passiveManager = GetComponent<PassiveManager>();
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (currencyTracker == null) currencyTracker = GetComponent<CurrencyTracker>();
        if (stats == null) stats = GetComponent<StatsManager>();
        if (characterLoader == null) characterLoader = GetComponent<CharacterLoader>();
    }

    private void Start()
    {
        if (!RunStateManager.Instance.HasData) return;

        RestoreInventory(RunStateManager.Instance.InventorySnapshot);
        RestoreCurrency(RunStateManager.Instance.CurrencySnapshot);

        if (passiveManager != null)
            passiveManager.RestoreSnapshot(RunStateManager.Instance.PassiveSnapshot);

        // After the passives, never before: they change MaxHealth, and SetCurrentHealth clamps.
        if (stats != null && RunStateManager.Instance.CurrentHealth > 0f)
            stats.SetCurrentHealth(RunStateManager.Instance.CurrentHealth);

        RunStateManager.Instance.Clear();
    }

    /// <summary>Call right before a scene transition that should carry the player's run state forward.</summary>
    public void CaptureAndStore()
    {
        PassiveManager.PassiveSnapshot passives = passiveManager != null ? passiveManager.CaptureSnapshot() : null;

        List<InventoryItem> items = inventory != null
            ? new List<InventoryItem>(inventory.InventorySlots)
            : new List<InventoryItem>();

        List<(CurrencySO currency, int amount)> currency = currencyTracker != null
            ? new List<(CurrencySO currency, int amount)>(currencyTracker.GetAllBalances())
            : new List<(CurrencySO, int)>();

        float health = stats != null ? stats.CurrentHealth : 0f;
        string characterId = characterLoader != null ? characterLoader.CurrentCharacterId : null;

        RunStateManager.Instance.Store(passives, items, currency, health, characterId);
    }

    private void RestoreInventory(List<InventoryItem> items)
    {
        if (inventory == null || items == null) return;

        inventory.InventorySlots.Clear();
        inventory.InventorySlots.AddRange(items);
        inventory.RefreshUI();
    }

    private void RestoreCurrency(List<(CurrencySO currency, int amount)> balances)
    {
        if (currencyTracker == null || balances == null) return;

        foreach ((CurrencySO currency, int amount) in balances)
            if (currency != null && amount > 0)
                currencyTracker.Add(currency, amount);
    }
}
