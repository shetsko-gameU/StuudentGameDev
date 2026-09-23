using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps the ScriptableObject assets a save file has to reference (ingredient/food templates,
/// the four passive families, ult foods, currencies) to stable string ids.
///
/// This exists because JsonUtility cannot serialize an asset reference and nothing in this
/// project carries its own id field. The ids are Unity asset GUIDs, so renaming or moving an
/// asset does not invalidate an existing save — only deleting one does.
///
/// The list is filled by Tools > Save System > Rebuild Id Registry (see
/// Assets/editor/SaveIdRegistryBuilder.cs). Re-run that after adding new passive, ingredient
/// or currency assets: anything unregistered is silently skipped when saving, so a player
/// would lose exactly those items on load.
///
/// The asset must stay at Assets/Resources/SaveIdRegistry.asset — it is loaded by name at
/// runtime so no scene has to hold a reference to it.
/// </summary>
public class SaveIdRegistry : ScriptableObject
{
    /// <summary>Resources-relative name, i.e. Assets/Resources/SaveIdRegistry.asset.</summary>
    public const string ResourceName = "SaveIdRegistry";

    [System.Serializable]
    public struct Entry
    {
        public string id;
        public ScriptableObject asset;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    /// <summary>For the editor rebuild command only — never mutate this at runtime.</summary>
    public List<Entry> EditorEntries => entries;

    public int Count => entries != null ? entries.Count : 0;

    private Dictionary<string, ScriptableObject> byId;
    private Dictionary<ScriptableObject, string> byAsset;

    private static SaveIdRegistry cached;
    private static bool loadAttempted;

    private static SaveIdRegistry Instance
    {
        get
        {
            // Cache the miss too, so a missing registry logs once instead of every lookup.
            if (cached == null && !loadAttempted)
            {
                loadAttempted = true;
                cached = Resources.Load<SaveIdRegistry>(ResourceName);

                if (cached == null)
                    Debug.LogError($"SaveIdRegistry: no asset found at Assets/Resources/{ResourceName}.asset. " +
                                   "Saved runs cannot resolve their items until you run " +
                                   "Tools > Save System > Rebuild Id Registry.");
            }

            return cached;
        }
    }

    /// <summary>The save id for an asset, or null when it isn't registered.</summary>
    public static string IdOf(ScriptableObject asset)
    {
        if (asset == null) return null;

        SaveIdRegistry registry = Instance;
        if (registry == null) return null;

        registry.EnsureMaps();
        return registry.byAsset.TryGetValue(asset, out string id) ? id : null;
    }

    /// <summary>The asset for a save id, or null when it's unknown or the wrong type.</summary>
    public static T Resolve<T>(string id) where T : ScriptableObject
    {
        if (string.IsNullOrEmpty(id)) return null;

        SaveIdRegistry registry = Instance;
        if (registry == null) return null;

        registry.EnsureMaps();
        if (!registry.byId.TryGetValue(id, out ScriptableObject asset))
            return null;

        return asset as T;
    }

    private void EnsureMaps()
    {
        if (byId != null && byAsset != null) return;

        byId = new Dictionary<string, ScriptableObject>();
        byAsset = new Dictionary<ScriptableObject, string>();

        if (entries == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.asset == null || string.IsNullOrEmpty(entry.id)) continue;

            // A duplicate means the registry was hand-edited or an asset was duplicated with
            // its GUID intact; keep the first and say so rather than throwing.
            if (byId.ContainsKey(entry.id))
            {
                Debug.LogWarning($"SaveIdRegistry: duplicate id '{entry.id}' " +
                                 $"(kept '{byId[entry.id].name}', ignored '{entry.asset.name}').");
                continue;
            }

            byId.Add(entry.id, entry.asset);
            byAsset[entry.asset] = entry.id;
        }
    }

    private void OnDisable()
    {
        // Domain reloads invalidate the cached maps; drop them so they rebuild on next use.
        byId = null;
        byAsset = null;
    }
}
