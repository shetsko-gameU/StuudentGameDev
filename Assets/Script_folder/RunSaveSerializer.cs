using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Converts between the live run state and the on-disk save file. Everything that writes or
/// resumes a save goes through here: RoomExit at each portal, AutoSaveHooks on quit, the hub
/// character swap, and MainMenuManager's Continue.
///
/// The design point is that RunStateManager is already the one in-memory snapshot of a run, and
/// RunStatePlayerLink already knows how to push that snapshot back onto a Player. So resuming a
/// saved run just means filling RunStateManager from disk before loading the scene — no second
/// restore path to keep in sync.
/// </summary>
public static class RunSaveSerializer
{
    // ------------------------------------------------------------------ Writing

    /// <summary>
    /// Snapshots the live Player into RunStateManager and writes it to disk, recording
    /// <paramref name="sceneName"/> as the stage to resume at. At a portal that is the
    /// destination scene, which is what makes a resumed run start at the stage the player
    /// was heading into.
    /// </summary>
    public static void CaptureAndWrite(string sceneName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        RunStatePlayerLink link = player != null ? player.GetComponent<RunStatePlayerLink>() : null;

        if (link == null)
        {
            Debug.LogWarning("RunSaveSerializer: no Player with RunStatePlayerLink in the scene — " +
                             "nothing to capture, save skipped.");
            return;
        }

        link.CaptureAndStore();
        WriteFromRunState(sceneName);
    }

    /// <summary>Writes whatever RunStateManager currently holds, without re-reading the Player.</summary>
    public static void WriteFromRunState(string sceneName)
    {
        RunStateManager state = RunStateManager.Instance;

        if (!state.HasData)
        {
            Debug.LogWarning("RunSaveSerializer: RunStateManager holds no snapshot — save skipped.");
            return;
        }

        SaveFile file = ReadOrNew();

        // Keep the existing character choice if this snapshot doesn't name one.
        string characterId = !string.IsNullOrEmpty(state.CharacterId)
            ? state.CharacterId
            : CharacterLoader.SavedCharacterId;

        if (!string.IsNullOrEmpty(characterId))
            file.characterId = characterId;

        file.run = new RunDto
        {
            sceneName = sceneName,
            currentHealth = state.CurrentHealth,
            inventory = InventoryToDto(state.InventorySnapshot),
            passives = PassivesToDto(state.PassiveSnapshot),
            currency = CurrencyToDto(state.CurrencySnapshot),
        };

        SaveSystem.Write(file);
    }

    /// <summary>
    /// Re-saves the current stage, but only if the save already points at it. Used by the
    /// quit/pause hook so closing the game in the hub or main menu can't invent a run, and
    /// can't move an existing run's resume point to a non-stage scene.
    /// </summary>
    public static void RefreshCurrentStage()
    {
        SaveFile file;
        if (!SaveSystem.TryRead(out file) || !file.HasRun) return;

        string active = SceneManager.GetActiveScene().name;
        if (file.run.sceneName != active) return;

        CaptureAndWrite(active);
    }

    /// <summary>Records the chosen character without touching the run, and remembers it for next launch.</summary>
    public static void WriteCharacterChoice(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return;

        CharacterLoader.SavedCharacterId = characterId;

        SaveFile file = ReadOrNew();
        file.characterId = characterId;
        SaveSystem.Write(file);
    }

    // ------------------------------------------------------------------ Reading

    /// <summary>
    /// Fills RunStateManager from the saved run and reports which scene to load. Call this
    /// immediately before SceneManager.LoadScene; RunStatePlayerLink.Start() does the rest.
    /// </summary>
    public static bool TryLoadIntoRunState(out string sceneName)
    {
        sceneName = null;

        SaveFile file;
        if (!SaveSystem.TryRead(out file) || !file.HasRun) return false;

        RunDto run = file.run;

        string characterId = !string.IsNullOrEmpty(file.characterId)
            ? file.characterId
            : CharacterLoader.SavedCharacterId;

        RunStateManager.Instance.Store(
            PassivesFromDto(run.passives),
            InventoryFromDto(run.inventory),
            CurrencyFromDto(run.currency),
            run.currentHealth,
            characterId);

        sceneName = run.sceneName;
        return true;
    }

    private static SaveFile ReadOrNew()
    {
        SaveFile file;
        return SaveSystem.TryRead(out file) ? file : new SaveFile();
    }

    // ------------------------------------------------------------------ Inventory

    private static List<InventoryItemDto> InventoryToDto(List<InventoryItem> items)
    {
        List<InventoryItemDto> dtos = new List<InventoryItemDto>();
        if (items == null) return dtos;

        foreach (InventoryItem item in items)
        {
            if (item == null) continue;

            string id = SaveIdRegistry.IdOf(item.ModifierSO);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"RunSaveSerializer: inventory item '{item.Name}' points at an " +
                                 "unregistered asset and is being dropped from the save. Run " +
                                 "Tools > Save System > Rebuild Id Registry.");
                continue;
            }

            dtos.Add(new InventoryItemDto
            {
                modifierId = id,
                name = item.Name,
                description = item.Description,
                itemId = item.ItemID,
            });
        }

        return dtos;
    }

    private static List<InventoryItem> InventoryFromDto(List<InventoryItemDto> dtos)
    {
        List<InventoryItem> items = new List<InventoryItem>();
        if (dtos == null) return items;

        foreach (InventoryItemDto dto in dtos)
        {
            StatsModifierSO so = SaveIdRegistry.Resolve<StatsModifierSO>(dto.modifierId);
            if (so == null)
            {
                Debug.LogWarning($"RunSaveSerializer: saved inventory item '{dto.name}' no longer " +
                                 "resolves to an asset — skipping it.");
                continue;
            }

            items.Add(new InventoryItem
            {
                ModifierSO = so,
                Name = dto.name,
                Description = dto.description,
                ItemID = dto.itemId,
                Image = so.Image,   // not saved; the asset is the source of truth for art
            });
        }

        return items;
    }

    // ------------------------------------------------------------------ Passives

    private static PassivesDto PassivesToDto(PassiveManager.PassiveSnapshot snap)
    {
        PassivesDto dto = new PassivesDto();
        if (snap == null) return dto;

        foreach (PassiveManager.PassiveSnapshot.FoodEntry e in snap.foodPassives)
            AddEntry(dto.foodPassives, e.passive, e.roll);

        foreach (PassiveManager.PassiveSnapshot.StatEntry e in snap.statBoosts)
            AddEntry(dto.statBoosts, e.passive, e.roll);

        foreach (PassiveManager.PassiveSnapshot.KillEntry e in snap.killPassives)
            AddEntry(dto.killPassives, e.passive, e.roll);

        foreach (PassiveManager.PassiveSnapshot.DebuffEntry e in snap.debuffPassives)
            AddEntry(dto.debuffPassives, e.passive, e.roll);

        dto.ultFoodId = SaveIdRegistry.IdOf(snap.ultFood);
        return dto;
    }

    private static void AddEntry(List<PassiveEntryDto> list, ScriptableObject passive, RolledModifierInstance roll)
    {
        if (passive == null) return;

        string id = SaveIdRegistry.IdOf(passive);
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"RunSaveSerializer: passive '{passive.name}' is unregistered and is " +
                             "being dropped from the save. Run Tools > Save System > Rebuild Id Registry.");
            return;
        }

        list.Add(new PassiveEntryDto { passiveId = id, roll = RollToDto(roll) });
    }

    private static PassiveManager.PassiveSnapshot PassivesFromDto(PassivesDto dto)
    {
        PassiveManager.PassiveSnapshot snap = new PassiveManager.PassiveSnapshot();
        if (dto == null) return snap;

        foreach (PassiveEntryDto e in dto.foodPassives)
        {
            OnHitPassiveSO passive = SaveIdRegistry.Resolve<OnHitPassiveSO>(e.passiveId);
            if (passive == null) { WarnMissingPassive(e.passiveId); continue; }
            snap.foodPassives.Add(new PassiveManager.PassiveSnapshot.FoodEntry
            { passive = passive, roll = RollFromDto(e.roll) });
        }

        foreach (PassiveEntryDto e in dto.statBoosts)
        {
            FoodStatPassiveSO passive = SaveIdRegistry.Resolve<FoodStatPassiveSO>(e.passiveId);
            if (passive == null) { WarnMissingPassive(e.passiveId); continue; }
            snap.statBoosts.Add(new PassiveManager.PassiveSnapshot.StatEntry
            { passive = passive, roll = RollFromDto(e.roll) });
        }

        foreach (PassiveEntryDto e in dto.killPassives)
        {
            KillPassiveSO passive = SaveIdRegistry.Resolve<KillPassiveSO>(e.passiveId);
            if (passive == null) { WarnMissingPassive(e.passiveId); continue; }
            snap.killPassives.Add(new PassiveManager.PassiveSnapshot.KillEntry
            { passive = passive, roll = RollFromDto(e.roll) });
        }

        foreach (PassiveEntryDto e in dto.debuffPassives)
        {
            DebuffOnHitPassiveSO passive = SaveIdRegistry.Resolve<DebuffOnHitPassiveSO>(e.passiveId);
            if (passive == null) { WarnMissingPassive(e.passiveId); continue; }
            snap.debuffPassives.Add(new PassiveManager.PassiveSnapshot.DebuffEntry
            { passive = passive, roll = RollFromDto(e.roll) });
        }

        snap.ultFood = SaveIdRegistry.Resolve<UltFoodSO>(dto.ultFoodId);
        return snap;
    }

    private static void WarnMissingPassive(string id)
    {
        Debug.LogWarning($"RunSaveSerializer: saved passive id '{id}' no longer resolves to an " +
                         "asset of the expected type — skipping it.");
    }

    // ------------------------------------------------------------------ Rolled modifiers

    private static RolledModifierDto RollToDto(RolledModifierInstance inst)
    {
        if (inst == null) return null;

        RolledModifierDto dto = new RolledModifierDto
        {
            sourceId = SaveIdRegistry.IdOf(inst.source),
            durationSeconds = inst.durationSeconds,
            stacks = inst.stacks,
        };

        foreach (KeyValuePair<(StatType stat, ModifierMode mode), float> pair in inst.stackableValues)
            dto.stackable.Add(ToStatValue(pair));

        foreach (KeyValuePair<(StatType stat, ModifierMode mode), float> pair in inst.nonStackableValues)
            dto.nonStackable.Add(ToStatValue(pair));

        return dto;
    }

    private static StatValueDto ToStatValue(KeyValuePair<(StatType stat, ModifierMode mode), float> pair)
    {
        return new StatValueDto { stat = pair.Key.stat, mode = pair.Key.mode, value = pair.Value };
    }

    /// <summary>
    /// Rebuilds a rolled instance from its saved numbers. The numbers have to be saved rather
    /// than re-rolled from the source asset: each line is rolled independently, so re-rolling
    /// would hand the player different stats than the ones they earned.
    /// </summary>
    private static RolledModifierInstance RollFromDto(RolledModifierDto dto)
    {
        if (dto == null) return null;

        RolledModifierInstance inst = new RolledModifierInstance
        {
            source = SaveIdRegistry.Resolve<StatsModifierSO>(dto.sourceId),
            durationSeconds = dto.durationSeconds,
            stacks = Mathf.Max(1, dto.stacks),
        };

        if (dto.stackable != null)
            foreach (StatValueDto v in dto.stackable)
                Accumulate(inst.stackableValues, inst.values, v);

        if (dto.nonStackable != null)
            foreach (StatValueDto v in dto.nonStackable)
                Accumulate(inst.nonStackableValues, inst.values, v);

        return inst;
    }

    private static void Accumulate(Dictionary<(StatType stat, ModifierMode mode), float> bucket,
                                   Dictionary<(StatType stat, ModifierMode mode), float> combined,
                                   StatValueDto v)
    {
        var key = (v.stat, v.mode);

        // 'values' is the sum of both buckets, matching how ModifierRoller builds it.
        bucket[key] = bucket.ContainsKey(key) ? bucket[key] + v.value : v.value;
        combined[key] = combined.ContainsKey(key) ? combined[key] + v.value : v.value;
    }

    // ------------------------------------------------------------------ Currency

    private static List<CurrencyDto> CurrencyToDto(List<(CurrencySO currency, int amount)> balances)
    {
        List<CurrencyDto> dtos = new List<CurrencyDto>();
        if (balances == null) return dtos;

        foreach ((CurrencySO currency, int amount) in balances)
        {
            string id = SaveIdRegistry.IdOf(currency);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"RunSaveSerializer: currency '{(currency != null ? currency.name : "null")}' " +
                                 "is unregistered and is being dropped from the save. Run " +
                                 "Tools > Save System > Rebuild Id Registry.");
                continue;
            }

            dtos.Add(new CurrencyDto { currencyId = id, amount = amount });
        }

        return dtos;
    }

    private static List<(CurrencySO currency, int amount)> CurrencyFromDto(List<CurrencyDto> dtos)
    {
        List<(CurrencySO currency, int amount)> balances = new List<(CurrencySO, int)>();
        if (dtos == null) return balances;

        foreach (CurrencyDto dto in dtos)
        {
            CurrencySO currency = SaveIdRegistry.Resolve<CurrencySO>(dto.currencyId);
            if (currency == null)
            {
                Debug.LogWarning($"RunSaveSerializer: saved currency id '{dto.currencyId}' no longer " +
                                 "resolves to an asset — skipping it.");
                continue;
            }

            balances.Add((currency, dto.amount));
        }

        return balances;
    }
}
