using System.IO;
using UnityEngine;

/// <summary>
/// Reads and writes the single save file at Application.persistentDataPath/save.json.
///
/// Writes go to a temp file that then replaces the real one, so a crash or power cut during a
/// write leaves the previous save intact instead of a half-written file — the whole point of
/// this system is surviving crashes, so it must not create one.
///
/// Nothing here knows about game objects; RunSaveSerializer owns the conversion between this
/// file and the live run state.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "save.json";
    private const string TempFileName = "save.json.tmp";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string TempPath => Path.Combine(Application.persistentDataPath, TempFileName);

    /// <summary>Full path to the save file — handy for logging where it actually landed.</summary>
    public static string Location => FilePath;

    public static bool Exists => File.Exists(FilePath);

    /// <summary>True when the file holds a run that can be resumed (as opposed to just a character choice).</summary>
    public static bool HasRun
    {
        get
        {
            SaveFile file;
            return TryRead(out file) && file.HasRun;
        }
    }

    public static bool TryRead(out SaveFile file)
    {
        file = null;

        if (!File.Exists(FilePath)) return false;

        try
        {
            string json = File.ReadAllText(FilePath);
            file = JsonUtility.FromJson<SaveFile>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem: could not read '{FilePath}' — {e.Message}");
            return false;
        }

        if (file == null)
        {
            Debug.LogError($"SaveSystem: '{FilePath}' is not valid save JSON — ignoring it.");
            return false;
        }

        if (file.version > SaveFile.CurrentVersion)
        {
            Debug.LogWarning($"SaveSystem: save was written by a newer version " +
                             $"({file.version} > {SaveFile.CurrentVersion}) — ignoring it rather than " +
                             "guessing at fields that may have changed meaning.");
            return false;
        }

        return true;
    }

    public static void Write(SaveFile file)
    {
        if (file == null) return;

        file.version = SaveFile.CurrentVersion;

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(TempPath, JsonUtility.ToJson(file, true));

            // File.Replace needs the destination to exist; first save is a plain move.
            if (File.Exists(FilePath))
                File.Replace(TempPath, FilePath, null);
            else
                File.Move(TempPath, FilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem: could not write '{FilePath}' — {e.Message}");
        }
    }

    /// <summary>
    /// Clears the in-progress run but keeps the chosen character. This is what death and a boss
    /// win call: the run is over, but the player shouldn't be handed a different body next time.
    /// </summary>
    public static void DeleteRun()
    {
        SaveFile file;
        if (!TryRead(out file)) return;

        if (!file.HasRun) return;

        file.run = new RunDto();
        Write(file);
    }

    /// <summary>Removes the whole file, character choice included. Not used by gameplay; for cleanup and tests.</summary>
    public static void DeleteAll()
    {
        try
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveSystem: could not delete '{FilePath}' — {e.Message}");
        }
    }
}
