using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Performance > Run Potato Scenes.
///
/// Plays hub, forest level 01, and boss arena in order. Each Play session runs
/// PotatoPerfHarness (2 s warmup + 10 s sample), appends the JSON summary, then stops
/// and moves on. Combined report is written next to the last single-scene file.
///
/// Scene order and budgets live on PotatoPerfHarness — this file only drives Play mode.
/// </summary>
public static class PotatoPerfRunner
{
    private const string QueueKey = "PotatoPerf.QueueIndex";
    private const string RunningKey = "PotatoPerf.Running";
    private const string ReportsKey = "PotatoPerf.Reports";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/hub.unity",
        "Assets/Scenes/forest level 01.unity",
        "Assets/Scenes/boss arena.unity",
    };

    [InitializeOnLoadMethod]
    private static void WatchPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.update -= PumpPlayMode;
        EditorApplication.update += PumpPlayMode;
    }

    /// <summary>
    /// Keeps Play mode ticking while the Editor is unfocused — Unity otherwise freezes
    /// frameCount at 2 and the harness never finishes. Safe to call more than once.
    /// </summary>
    public static void EnablePlayPump()
    {
        EditorApplication.update -= PumpPlayMode;
        EditorApplication.update += PumpPlayMode;
    }

    public static void DisablePlayPump()
    {
        EditorApplication.update -= PumpPlayMode;
    }

    private static void PumpPlayMode()
    {
        if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            EditorApplication.QueuePlayerLoopUpdate();
    }

    /// <summary>
    /// Times real wall-clock work per Play-mode Step. Unity's paused Step reports a fake
    /// 20 ms deltaTime, which would always fail the 11 ms proxy — so we stopwatch the
    /// Step itself instead. Call this while already in Play mode.
    /// </summary>
    public static PotatoPerfHarness.Report SampleStepped(int warmupFrames, int sampleFrames)
    {
        if (!EditorApplication.isPlaying)
            throw new System.InvalidOperationException("SampleStepped requires Play mode.");

        EditorApplication.isPaused = true;

        for (int i = 0; i < warmupFrames; i++)
            EditorApplication.Step();

        var times = new List<float>(sampleFrames);
        var sw = new System.Diagnostics.Stopwatch();
        int maxEnemies = 0;
        int maxAnimators = 0;

        for (int i = 0; i < sampleFrames; i++)
        {
            sw.Reset();
            sw.Start();
            EditorApplication.Step();
            sw.Stop();
            times.Add((float)sw.Elapsed.TotalMilliseconds);

            int enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length;
            int animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Length;
            if (enemies > maxEnemies) maxEnemies = enemies;
            if (animators > maxAnimators) maxAnimators = animators;
        }

        float avg = 0f;
        float max = 0f;
        for (int i = 0; i < times.Count; i++)
        {
            avg += times[i];
            if (times[i] > max) max = times[i];
        }

        if (times.Count > 0) avg /= times.Count;
        float[] sorted = times.ToArray();
        System.Array.Sort(sorted);
        float p95 = sorted.Length > 0
            ? sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * 0.95f) - 1, 0, sorted.Length - 1)]
            : 0f;

        var reasons = new List<string>();
        if (p95 > PotatoPerfHarness.PotatoP95Ms)
            reasons.Add("p95 " + p95.ToString("F1") + "ms > " + PotatoPerfHarness.PotatoP95Ms + "ms");
        if (max > PotatoPerfHarness.PotatoHitchMs)
            reasons.Add("hitch " + max.ToString("F1") + "ms > " + PotatoPerfHarness.PotatoHitchMs + "ms");

        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene.StartsWith("forest") && maxEnemies < 1)
            reasons.Add("no enemies spawned — combat sample is invalid");

        var report = new PotatoPerfHarness.Report
        {
            sceneName = scene,
            machine = SystemInfo.graphicsDeviceName + " / " + SystemInfo.processorType,
            warmupSeconds = warmupFrames / 60f,
            sampleSeconds = sampleFrames / 60f,
            frames = times.Count,
            avgMs = avg,
            p95Ms = p95,
            maxMs = max,
            avgFps = avg > 0f ? 1000f / avg : 0f,
            gcKbPerFrame = 0f,
            maxEnemies = maxEnemies,
            maxAnimators = maxAnimators,
            potatoPass = reasons.Count == 0,
            proxyPass = p95 <= PotatoPerfHarness.ProxyP95Ms && (reasons.Count == 0 || reasons.TrueForAll(r => !r.StartsWith("no enemies"))),
            failReasons = reasons.Count == 0 ? "" : string.Join("; ", reasons),
        };

        Debug.Log(PotatoPerfHarness.FormatSummary(report));
        return report;
    }

    [MenuItem("Tools/Performance/Run Potato Scenes")]
    public static void RunPotatoScenes()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("PotatoPerfRunner: stop Play mode before running the suite.");
            return;
        }

        SessionState.SetInt(QueueKey, 0);
        SessionState.SetBool(RunningKey, true);
        SessionState.SetString(ReportsKey, "");
        StartQueuedScene();
    }

    [MenuItem("Tools/Performance/Run Potato Scenes (Current Scene)")]
    public static void RunCurrentScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("PotatoPerfRunner: already in Play mode.");
            return;
        }

        PlayerPrefs.SetInt(PotatoPerfHarness.AutoRunPrefsKey, 1);
        PlayerPrefs.Save();
        SessionState.SetBool(RunningKey, false);
        EnablePlayPump();
        EditorApplication.isPlaying = true;
    }

    private static void StartQueuedScene()
    {
        int index = SessionState.GetInt(QueueKey, 0);
        if (index < 0 || index >= Scenes.Length)
        {
            FinishSuite();
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            SessionState.SetBool(RunningKey, false);
            return;
        }

        EditorSceneManager.OpenScene(Scenes[index], OpenSceneMode.Single);
        PlayerPrefs.SetInt(PotatoPerfHarness.AutoRunPrefsKey, 1);
        PlayerPrefs.Save();
        Debug.Log("PotatoPerfRunner: playing " + Scenes[index] + " (" + (index + 1) + "/" + Scenes.Length + ")");
        EnablePlayPump();
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;

        if (change == PlayModeStateChange.EnteredPlayMode)
            EditorApplication.update += TickPlayMode;

        if (change == PlayModeStateChange.ExitingPlayMode)
            EditorApplication.update -= TickPlayMode;

        if (change == PlayModeStateChange.EnteredEditMode)
        {
            int next = SessionState.GetInt(QueueKey, 0) + 1;
            SessionState.SetInt(QueueKey, next);
            EditorApplication.delayCall += StartQueuedScene;
        }
    }

    private static void TickPlayMode()
    {
        if (!EditorApplication.isPlaying) return;
        if (!PotatoPerfHarness.IsFinished) return;

        if (PotatoPerfHarness.LastReport != null)
        {
            string existing = SessionState.GetString(ReportsKey, "");
            string line = PotatoPerfHarness.FormatSummary(PotatoPerfHarness.LastReport);
            SessionState.SetString(ReportsKey, string.IsNullOrEmpty(existing) ? line : existing + "\n" + line);
        }

        EditorApplication.isPlaying = false;
    }

    private static void FinishSuite()
    {
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= TickPlayMode;

        string reports = SessionState.GetString(ReportsKey, "");
        string path = Path.Combine(Application.persistentDataPath, "perf-suite.txt");
        try
        {
            File.WriteAllText(path, reports);
        }
        catch (System.Exception e)
        {
            Debug.LogError("PotatoPerfRunner: could not write suite report — " + e.Message);
        }

        Debug.Log("PotatoPerfRunner: suite finished.\n" + reports + "\nWrote " + path);
    }
}
