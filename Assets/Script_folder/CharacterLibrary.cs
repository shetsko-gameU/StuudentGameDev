using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The list of playable characters, so a saved id can be turned back into a CharacterSO without
/// any scene holding a reference to every character asset.
///
/// Must live at Assets/Resources/CharacterLibrary.asset — it is loaded by name at runtime. List
/// order is the selection order, so it also decides which body a brand new player starts as.
/// </summary>
public class CharacterLibrary : ScriptableObject
{
    public const string ResourceName = "CharacterLibrary";

    [Tooltip("Every playable character. The first entry is the default for a player with no saved choice.")]
    public List<CharacterSO> characters = new List<CharacterSO>();

    private static CharacterLibrary cached;
    private static bool loadAttempted;

    public static CharacterLibrary Instance
    {
        get
        {
            if (cached == null && !loadAttempted)
            {
                loadAttempted = true;
                cached = Resources.Load<CharacterLibrary>(ResourceName);

                if (cached == null)
                    Debug.LogError($"CharacterLibrary: no asset found at Assets/Resources/{ResourceName}.asset — " +
                                   "character swapping and saved character choices will not work.");
            }

            return cached;
        }
    }

    /// <summary>The character with this id, or null when it's empty or unknown.</summary>
    public static CharacterSO Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        CharacterLibrary library = Instance;
        if (library == null) return null;

        foreach (CharacterSO character in library.characters)
            if (character != null && character.id == id)
                return character;

        return null;
    }

    /// <summary>First entry in the list — what a player with no saved choice gets.</summary>
    public static CharacterSO Default
    {
        get
        {
            CharacterLibrary library = Instance;
            if (library == null) return null;

            foreach (CharacterSO character in library.characters)
                if (character != null)
                    return character;

            return null;
        }
    }

    /// <summary>The next character in list order, wrapping around. Used by the hub swap station.</summary>
    public static CharacterSO Next(CharacterSO current)
    {
        CharacterLibrary library = Instance;
        if (library == null || library.characters.Count == 0) return null;

        int index = current != null ? library.characters.IndexOf(current) : -1;

        for (int step = 1; step <= library.characters.Count; step++)
        {
            CharacterSO candidate = library.characters[(index + step + library.characters.Count) % library.characters.Count];
            if (candidate != null && candidate != current)
                return candidate;
        }

        return null;
    }
}
