using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Samples frame time, GC, and live enemy/animator counts so we can tell whether a scene
/// will hold 30 FPS on an integrated-GPU laptop.
///
/// Two pass lines are recorded because a 30 FPS result on a development PC does not prove
/// a potato will hold:
///   - Potato budget: p95 under 33.3 ms, no hitch over 50 ms, GC under 50 KB/frame.
///   - Dev-PC proxy: p95 under 11 ms (~90 FPS). If this fails here, the potato will fail.
///
/// After a 2 s warmup (drops the load hitch) it records 10 s, writes
/// Application.persistentDataPath/perf-last.json, and logs one summary line.
///
/// Tools > Performance > Run Potato Scenes drives this from the Editor. On a real potato,
/// add this component to any scene (or leave PotatoPerf_AutoRun=1 in PlayerPrefs) and read
/// the JSON after Play.
/// </summary>
public class PotatoPerfHarness : MonoBehaviour
{
    public const string AutoRunPrefsKey = "PotatoPerf_AutoRun";
    public const string ForceEnemyPrefsKey = "PotatoPerf_ForceEnemyCount";
    public const string LastReportFile = "perf-last.json";

    public const float PotatoP95Ms = 33.3f;
    public const float PotatoHitchMs = 50f;
    public const float PotatoGcKbPerFrame = 50f;
    public const float ProxyP95Ms = 11f;

    [Header("Timing")]
    public float warmupSeconds = 2f;
    public float sampleSeconds = 10f;

    [Header("Forest stress (dev PC only)")]
    [Tooltip("Extra enemies to spawn after the real waves start. 0 means production counts only.")]
    public int forceEnemyCount;

    [System.Serializable]
    public class Report
    {
        public string sceneName;
        public string machine;
        public float warmupSeconds;
        public float sampleSeconds;
        public int frames;
        public float avgMs;
        public float p95Ms;
        public float maxMs;
        public float avgFps;
        public float gcKbPerFrame;
        public int maxEnemies;
        public int maxAnimators;
        public bool potatoPass;
        public bool proxyPass;
        public string failReasons;
    }

    public static Report LastReport { get; private set; }
    public static bool IsFinished { get; private set; }

    private readonly List<float> frameMs = new List<float>(600);
    private float warmupLeft;
    private float sampleLeft;
    private bool sampling;
    private long monoAtSampleStart;
    private long gcAllocBytes;
    private int maxEnemies;
    private int maxAnimators;
    private ProfilerRecorder gcRecorder;
    private bool gcRecorderRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawn()
    {
        if (PlayerPrefs.GetInt(AutoRunPrefsKey, 0) != 1) return;
        if (Object.FindFirstObjectByType<PotatoPerfHarness>() != null) return;

        GameObject host = new GameObject("PotatoPerfHarness");
        PotatoPerfHarness harness = host.AddComponent<PotatoPerfHarness>();
        harness.forceEnemyCount = PlayerPrefs.GetInt(ForceEnemyPrefsKey, 0);
        DontDestroyOnLoad(host);
    }

    private void Start()
    {
        IsFinished = false;
        LastReport = null;
        warmupLeft = Mathf.Max(0.1f, warmupSeconds);
        sampleLeft = Mathf.Max(0.1f, sampleSeconds);
        PrepareScene();
        TryStartGcRecorder();
    }

    private void OnDestroy()
    {
        if (gcRecorderRunning)
            gcRecorder.Dispose();
    }

    private void Update()
    {
        if (IsFinished) return;

        KeepPlayerAlive();

        if (!sampling)
        {
            warmupLeft -= Time.unscaledDeltaTime;
            if (warmupLeft > 0f) return;

            sampling = true;
            monoAtSampleStart = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            return;
        }

        float dtMs = Time.unscaledDeltaTime * 1000f;
        frameMs.Add(dtMs);
        if (gcRecorderRunning && gcRecorder.Valid)
            gcAllocBytes += gcRecorder.LastValue;

        CountLiveLoad();

        sampleLeft -= Time.unscaledDeltaTime;
        if (sampleLeft > 0f) return;

        Finish();
    }

    /// <summary>Kicks the production systems this scenario is meant to light up.</summary>
    private void PrepareScene()
    {
        string scene = SceneManager.GetActiveScene().name;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && !player.activeSelf)
            player.SetActive(true);

        if (scene == "hub")
            StartCoroutine(WalkHub(player));

        if (scene.StartsWith("forest"))
            ForceForestCombat();
    }

    private System.Collections.IEnumerator WalkHub(GameObject player)
    {
        if (player == null) yield break;

        Transform target = null;
        GameObject dirk = GameObject.Find("Dirk Pekkanen");
        if (dirk != null) target = dirk.transform;
        if (target == null)
        {
            GameObject portal = GameObject.Find("portal frame");
            if (portal != null) target = portal.transform;
        }

        if (target == null) yield break;

        Vector3 start = player.transform.position;
        Vector3 end = target.position;
        end.y = start.y;

        float duration = warmupSeconds + sampleSeconds;
        float t = 0f;
        while (t < duration && player != null)
        {
            t += Time.unscaledDeltaTime;
            player.transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(t / duration));
            yield return null;
        }
    }

    private void ForceForestCombat()
    {
        EnemyManager[] rooms = Object.FindObjectsByType<EnemyManager>(FindObjectsSortMode.None);
        List<GameObject> prefabs = new List<GameObject>();

        foreach (EnemyManager room in rooms)
        {
            room.ForceStart();
            room.SpawnTimer = 0f;
            room.SpawnTimerMax = 0.05f;

            if (room.EnemyTypes == null) continue;
            foreach (GameObject prefab in room.EnemyTypes)
                if (prefab != null && !prefabs.Contains(prefab))
                    prefabs.Add(prefab);
        }

        if (forceEnemyCount <= 0 || prefabs.Count == 0) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 origin = player != null ? player.transform.position : Vector3.zero;

        for (int i = 0; i < forceEnemyCount; i++)
        {
            GameObject prefab = prefabs[i % prefabs.Count];
            Vector3 pos = origin + new Vector3(Mathf.Cos(i * 0.9f) * 4f, 0f, Mathf.Sin(i * 0.9f) * 4f);
            Object.Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    private void KeepPlayerAlive()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        StatsManager stats = player.GetComponent<StatsManager>();
        if (stats != null && stats.CurrentHealth < stats.MaxHealth * 0.5f)
            stats.SetCurrentHealth(stats.MaxHealth);
    }

    private void CountLiveLoad()
    {
        int enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Length;
        int animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).Length;
        if (enemies > maxEnemies) maxEnemies = enemies;
        if (animators > maxAnimators) maxAnimators = animators;
    }

    private void TryStartGcRecorder()
    {
        try
        {
            gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC.Alloc");
            gcRecorderRunning = gcRecorder.Valid;
        }
        catch (System.Exception)
        {
            gcRecorderRunning = false;
        }
    }

    private void Finish()
    {
        IsFinished = true;
        if (gcRecorderRunning)
        {
            gcRecorder.Dispose();
            gcRecorderRunning = false;
        }

        Report report = BuildReport();
        LastReport = report;
        WriteReport(report);
        Debug.Log(FormatSummary(report));

        PlayerPrefs.SetInt(AutoRunPrefsKey, 0);
        PlayerPrefs.Save();
    }

    private Report BuildReport()
    {
        int n = frameMs.Count;
        float avg = 0f;
        float max = 0f;
        for (int i = 0; i < n; i++)
        {
            avg += frameMs[i];
            if (frameMs[i] > max) max = frameMs[i];
        }

        if (n > 0) avg /= n;

        float p95 = avg;
        if (n > 0)
        {
            float[] sorted = frameMs.ToArray();
            System.Array.Sort(sorted);
            p95 = sorted[Mathf.Clamp(Mathf.CeilToInt(n * 0.95f) - 1, 0, n - 1)];
        }

        float gcKb;
        if (gcAllocBytes > 0 && n > 0)
            gcKb = (gcAllocBytes / 1024f) / n;
        else
        {
            long monoNow = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            float deltaKb = Mathf.Max(0f, (monoNow - monoAtSampleStart) / 1024f);
            gcKb = n > 0 ? deltaKb / n : 0f;
        }

        var reasons = new List<string>();
        if (p95 > PotatoP95Ms) reasons.Add("p95 " + p95.ToString("F1") + "ms > " + PotatoP95Ms + "ms");
        if (max > PotatoHitchMs) reasons.Add("hitch " + max.ToString("F1") + "ms > " + PotatoHitchMs + "ms");
        if (gcKb > PotatoGcKbPerFrame) reasons.Add("gc " + gcKb.ToString("F1") + "KB/f > " + PotatoGcKbPerFrame + "KB/f");

        string scene = SceneManager.GetActiveScene().name;
        if (scene.StartsWith("forest") && maxEnemies < 1)
            reasons.Add("no enemies spawned — combat sample is invalid");

        return new Report
        {
            sceneName = scene,
            machine = SystemInfo.graphicsDeviceName + " / " + SystemInfo.processorType,
            warmupSeconds = warmupSeconds,
            sampleSeconds = sampleSeconds,
            frames = n,
            avgMs = avg,
            p95Ms = p95,
            maxMs = max,
            avgFps = avg > 0f ? 1000f / avg : 0f,
            gcKbPerFrame = gcKb,
            maxEnemies = maxEnemies,
            maxAnimators = maxAnimators,
            potatoPass = reasons.Count == 0,
            proxyPass = p95 <= ProxyP95Ms && reasons.Find(r => r.StartsWith("no enemies")) == null,
            failReasons = reasons.Count == 0 ? "" : string.Join("; ", reasons),
        };
    }

    public static string ReportPath => Path.Combine(Application.persistentDataPath, LastReportFile);

    private static void WriteReport(Report report)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
        catch (System.Exception e)
        {
            Debug.LogError("PotatoPerfHarness: could not write " + ReportPath + " — " + e.Message);
        }
    }

    public static string FormatSummary(Report report)
    {
        if (report == null) return "PotatoPerf: no report.";

        string potato = report.potatoPass ? "PASS" : "FAIL";
        string proxy = report.proxyPass ? "PASS" : "FAIL";
        return "PotatoPerf [" + report.sceneName + "] potato=" + potato + " proxy=" + proxy
               + " p95=" + report.p95Ms.ToString("F1") + "ms avg=" + report.avgMs.ToString("F1")
               + "ms max=" + report.maxMs.ToString("F1") + "ms fps=" + report.avgFps.ToString("F0")
               + " gc=" + report.gcKbPerFrame.ToString("F1") + "KB/f enemies=" + report.maxEnemies
               + " animators=" + report.maxAnimators
               + (string.IsNullOrEmpty(report.failReasons) ? "" : " (" + report.failReasons + ")");
    }
}
