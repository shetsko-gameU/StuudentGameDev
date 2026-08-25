using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent, scene-independent holder for run-scoped Player state (passives, inventory,
/// currency) — kept alive in memory only (nothing touches disk here) across a
/// SceneManager.LoadScene call so a mid-run scene transition, e.g. RoomExit's portal,
/// doesn't wipe the player back to their prefab defaults. Lazily creates itself on first
/// access so no scene needs to be manually set up with it.
///
/// Deliberately NOT the same thing as meta-progression persistence (currency/unlocks that
/// survive death or quitting) — this only survives until RunStatePlayerLink.Start() consumes
/// and clears it on the next scene, or Clear() is called explicitly (e.g. on player death).
/// </summary>
public class RunStateManager : MonoBehaviour
{
    private static RunStateManager instance;

    public static RunStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("RunStateManager (Persistent)");
                instance = go.AddComponent<RunStateManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public PassiveManager.PassiveSnapshot PassiveSnapshot { get; private set; }
    public List<InventoryItem> InventorySnapshot { get; private set; }
    public List<(CurrencySO currency, int amount)> CurrencySnapshot { get; private set; }
    public bool HasData { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Store(PassiveManager.PassiveSnapshot passives, List<InventoryItem> inventory,
                       List<(CurrencySO currency, int amount)> currency)
    {
        PassiveSnapshot = passives;
        InventorySnapshot = inventory;
        CurrencySnapshot = currency;
        HasData = true;
    }

    /// <summary>Discards any stored snapshot — call on death or whenever a run should start clean.</summary>
    public void Clear()
    {
        PassiveSnapshot = null;
        InventorySnapshot = null;
        CurrencySnapshot = null;
        HasData = false;
    }
}
