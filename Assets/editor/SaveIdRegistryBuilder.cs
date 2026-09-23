using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Save System > Rebuild Id Registry.
///
/// Scans the project for every ScriptableObject type a save file can point at and records each
/// asset's GUID into Assets/Resources/SaveIdRegistry.asset, creating the asset (and the
/// Resources folder) the first time.
///
/// Run this after adding or deleting passive / ingredient / currency assets. An asset that
/// isn't in the registry is skipped when writing a save, so the player would come back from a
/// resumed run missing exactly those items.
/// </summary>
public static class SaveIdRegistryBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string RegistryPath = ResourcesFolder + "/" + SaveIdRegistry.ResourceName + ".asset";

    // Every type a save file references. StatsModifierSO covers both inventory items and the
    // 'source' of each rolled modifier; the rest are the passive families PassiveManager
    // snapshots plus currencies.
    private static readonly System.Type[] TrackedTypes =
    {
        typeof(StatsModifierSO),
        typeof(OnHitPassiveSO),
        typeof(FoodStatPassiveSO),
        typeof(KillPassiveSO),
        typeof(DebuffOnHitPassiveSO),
        typeof(UltFoodSO),
        typeof(CurrencySO),
    };

    [MenuItem("Tools/Save System/Rebuild Id Registry")]
    public static void Rebuild()
    {
        SaveIdRegistry registry = AssetDatabase.LoadAssetAtPath<SaveIdRegistry>(RegistryPath);

        if (registry == null)
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");

            registry = ScriptableObject.CreateInstance<SaveIdRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            Debug.Log($"SaveIdRegistryBuilder: created {RegistryPath}.");
        }

        List<SaveIdRegistry.Entry> entries = registry.EditorEntries;
        entries.Clear();

        HashSet<string> seen = new HashSet<string>();
        System.Text.StringBuilder perType = new System.Text.StringBuilder();

        foreach (System.Type type in TrackedTypes)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + type.Name);
            int added = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // FindAssets matches subclasses as well, so load as the exact type and skip
                // anything that isn't actually it — otherwise an asset could land twice.
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
                if (asset == null) continue;
                if (!seen.Add(guid)) continue;

                entries.Add(new SaveIdRegistry.Entry { id = guid, asset = asset });
                added++;
            }

            perType.Append($"\n  {type.Name}: {added}");
        }

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        Debug.Log($"SaveIdRegistryBuilder: registered {entries.Count} assets.{perType}");
    }
}
