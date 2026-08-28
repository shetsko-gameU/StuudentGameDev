using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public List <GameObject> SpawnLocations = new List<GameObject>();
    public List <GameObject> TempSpawnLocations = new List<GameObject>();
    public List<EnemyBase> EnemiesInWave = new List<EnemyBase>();
    public StatsManager PlayerStats;
    

    [Tooltip("Pool of enemy prefabs this manager can spawn. Each spawn picks one at random, " +
             "the same way a random SpawnLocation is picked.")]
    public GameObject[] EnemyTypes;
    public float SpawnTimer;
    public float SpawnTimerMax;

    public List<int> EnemiesPerWave;

    public bool Active;
    public int EnemyWave;
    public int EnemyWaveMax;

    /// <summary>Fires once, the moment the last configured wave has no enemies left. Lets
    /// other systems (e.g. RoomExit) react without EnemyManager needing to know what they do.</summary>
    public event System.Action OnAllWavesCleared;

    /// <summary>Fires once, the moment the player first triggers this room (Active flips false → true).
    /// Lets other systems (e.g. RoomMusicTrigger) react without EnemyManager needing to know what they do.</summary>
    public event System.Action OnCombatStarted;

    private bool allWavesCleared;

    private void Awake()
    {
        

        if (EnemyTypes == null || EnemyTypes.Length == 0)
            Debug.LogError($"EnemyManager on '{name}': No EnemyTypes assigned.");

        if (SpawnLocations == null || SpawnLocations.Count == 0)
            Debug.LogError($"EnemyManager on '{name}': No SpawnLocations assigned.");

        if (EnemiesPerWave == null || EnemiesPerWave.Count == 0)
            Debug.LogError($"EnemyManager on '{name}': EnemiesPerWave is empty — no waves configured.");
        foreach (GameObject Location in SpawnLocations)
        {
            TempSpawnLocations.Add(Location);
        }
        GetComponent<MeshRenderer>().enabled = false;

    }

    // Update is called once per frame
    private void Update()
    {
        if (!Active) return;
        if (allWavesCleared) return;


        if (EnemyTypes == null || EnemyTypes.Length == 0 || SpawnLocations == null || SpawnLocations.Count == 0 || EnemiesPerWave == null)
            return;

        if (EnemyWave >= EnemiesPerWave.Count)
        {
            allWavesCleared = true;
            Debug.Log($"EnemyManager on '{name}': All waves cleared.");
            OnAllWavesCleared?.Invoke();
            return;
        }

        SpawnTimer -= Time.deltaTime;
        if (SpawnTimer <= 0)
        {
            if (EnemiesPerWave[EnemyWave] > 0)
            {
                SpawnEnemy();
                EnemiesPerWave[EnemyWave] -= 1;
                SpawnTimer = SpawnTimerMax;
            }
        }

        if (EnemiesPerWave[EnemyWave] <= 0 && EnemiesInWave.Count == 0)
        {
            EnemyWave += 1;
            TempSpawnLocations.Clear();
            foreach (GameObject Location in SpawnLocations)
            {
                TempSpawnLocations.Add(Location);
            }
        }
    }

    private void SpawnEnemy()
    {
        GameObject enemyPrefab = EnemyTypes[Random.Range(0, EnemyTypes.Length)];
        int RandomPostion = Random.Range(0, TempSpawnLocations.Count);
        Vector3 spawnPos = TempSpawnLocations[RandomPostion].transform.position;
        TempSpawnLocations[RandomPostion].GetComponentInChildren<ParticleSystem>().Play();
        TempSpawnLocations.Remove(TempSpawnLocations[RandomPostion]);

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, enemyPrefab.transform.rotation);

        EnemyBase enemyBase = newEnemy.GetComponent<EnemyBase>();
        if (enemyBase == null)
        {
            Debug.LogWarning($"EnemyManager on '{name}': Spawned '{enemyPrefab.name}' has no EnemyBase component — it won't be tracked and will block this wave from ever completing.");
            return;
        }

        EnemiesInWave.Add(enemyBase);

        if (enemyBase.stats != null)
            enemyBase.stats.OnDied += () => HandleEnemyDied(enemyBase);
        else
            Debug.LogWarning($"EnemyManager on '{name}': Spawned enemy '{newEnemy.name}' has no StatsManager assigned on its EnemyBase — it will never be removed from the wave when it dies.");
        enemyBase.Player_Stats = PlayerStats;
    }

    // Called when a tracked enemy's StatsManager fires OnDied. Without this, EnemiesInWave
    // never empties and EnemyWave can never advance past the first wave.
    private void HandleEnemyDied(EnemyBase enemyBase)
    {
        EnemiesInWave.Remove(enemyBase);

        if (enemyBase != null)
            Destroy(enemyBase.gameObject);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (!Active)
                OnCombatStarted?.Invoke();

            Active = true;
        }

    }



}
