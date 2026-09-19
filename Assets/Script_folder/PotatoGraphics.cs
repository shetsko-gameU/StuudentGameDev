using UnityEngine;

/// <summary>
/// Turns the Potato quality level on or off and caps the frame rate so an integrated GPU
/// is not drawing unused 200 FPS frames.
///
/// The CPU proxy on a development PC can pass while shadows still melt an Intel UHD, so
/// this preset exists even when the harness is green here. Integrated GPUs default to
/// Potato on first launch; a discrete GPU stays on PC until the player opts in.
///
/// PlayerPrefs key matches SettingsMenu's Settings_* pattern.
/// </summary>
public static class PotatoGraphics
{
    public const string PrefsKey = "Settings_PotatoGraphics";
    public const string QualityName = "Potato";
    public const int PotatoFrameRate = 30;

    public static bool IsEnabled
    {
        get { return PlayerPrefs.GetInt(PrefsKey, ShouldDefaultPotato() ? 1 : 0) == 1; }
        set
        {
            PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply(value);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySaved()
    {
        Apply(IsEnabled);
    }

    public static void Apply(bool potato)
    {
        int potatoIndex = IndexOf(QualityName);
        int pcIndex = IndexOf("PC");

        if (potato && potatoIndex >= 0)
            QualitySettings.SetQualityLevel(potatoIndex, true);
        else if (!potato && pcIndex >= 0)
            QualitySettings.SetQualityLevel(pcIndex, true);

        Application.targetFrameRate = potato ? PotatoFrameRate : -1;
    }

    public static bool ShouldDefaultPotato()
    {
        string gpu = SystemInfo.graphicsDeviceName;
        if (string.IsNullOrEmpty(gpu)) return true;

        string g = gpu.ToLowerInvariant();
        if (g.Contains("intel") || g.Contains("uhd") || g.Contains("iris") || g.Contains("hd graphics"))
            return true;
        if (g.Contains("vega") || g.Contains("radeon graphics"))
            return true;
        if (g.Contains("mali") || g.Contains("adreno") || g.Contains("apple"))
            return true;

        return false;
    }

    private static int IndexOf(string name)
    {
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
            if (names[i] == name)
                return i;
        return -1;
    }
}
