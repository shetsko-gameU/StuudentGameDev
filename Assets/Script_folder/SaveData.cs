using System.Collections.Generic;

/// <summary>
/// Plain serializable mirrors of the run state, shaped for JsonUtility. Written and read by
/// SaveSystem; converted to and from the live objects by RunSaveSerializer.
///
/// Two constraints drove these shapes:
///   - ScriptableObject references become string ids from SaveIdRegistry, since JsonUtility
///     cannot serialize an asset reference.
///   - RolledModifierInstance keys its values by a (StatType, ModifierMode) tuple, which
///     JsonUtility cannot serialize either, so those dictionaries are flattened into lists.
///
/// Keep every field public and non-readonly, and avoid properties: JsonUtility ignores
/// anything Unity's serializer would ignore.
/// </summary>
[System.Serializable]
public class SaveFile
{
    /// <summary>Bump when a field's meaning changes, so old files can be migrated or discarded.</summary>
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    /// <summary>CharacterSO.id last chosen in the hub. Survives death; only the run is wiped.</summary>
    public string characterId;

    public RunDto run = new RunDto();

    /// <summary>
    /// True when there is a run worth resuming. Checked via sceneName rather than a null test
    /// because Unity's serializer turns a null reference into a default-constructed instance.
    /// </summary>
    public bool HasRun => run != null && !string.IsNullOrEmpty(run.sceneName);
}

/// <summary>
/// One in-progress run, saved at stage boundaries. Deliberately holds no enemy, loot or
/// position data: resuming restarts the recorded stage, so all of that is rebuilt by the scene.
/// </summary>
[System.Serializable]
public class RunDto
{
    /// <summary>Scene to load on resume — the stage the player was entering or standing in.</summary>
    public string sceneName;

    public float currentHealth;

    public List<InventoryItemDto> inventory = new List<InventoryItemDto>();
    public PassivesDto passives = new PassivesDto();
    public List<CurrencyDto> currency = new List<CurrencyDto>();
}

/// <summary>
/// An uneaten inventory slot. Image is not saved — it is re-resolved from the modifier asset on
/// load — and RolledInstance is not either, since items are only rolled at the moment they're
/// eaten.
/// </summary>
[System.Serializable]
public class InventoryItemDto
{
    public string modifierId;
    public string name;
    public string description;
    public int itemId;
}

/// <summary>Mirrors PassiveManager.PassiveSnapshot, one list per passive family.</summary>
[System.Serializable]
public class PassivesDto
{
    public List<PassiveEntryDto> foodPassives = new List<PassiveEntryDto>();
    public List<PassiveEntryDto> statBoosts = new List<PassiveEntryDto>();
    public List<PassiveEntryDto> killPassives = new List<PassiveEntryDto>();
    public List<PassiveEntryDto> debuffPassives = new List<PassiveEntryDto>();
    public string ultFoodId;
}

/// <summary>A passive plus the permanent stat roll it granted when eaten.</summary>
[System.Serializable]
public class PassiveEntryDto
{
    public string passiveId;
    public RolledModifierDto roll;
}

/// <summary>
/// An already-rolled modifier. The rolled numbers must be saved, not just the source id:
/// ModifierRoller draws each line independently, so re-rolling on load would hand the player
/// different stats than they earned.
/// </summary>
[System.Serializable]
public class RolledModifierDto
{
    public string sourceId;
    public float durationSeconds;
    public int stacks = 1;

    public List<StatValueDto> stackable = new List<StatValueDto>();
    public List<StatValueDto> nonStackable = new List<StatValueDto>();
}

/// <summary>One flattened entry from a RolledModifierInstance value dictionary.</summary>
[System.Serializable]
public class StatValueDto
{
    public StatType stat;
    public ModifierMode mode;
    public float value;
}

[System.Serializable]
public class CurrencyDto
{
    public string currencyId;
    public int amount;
}
