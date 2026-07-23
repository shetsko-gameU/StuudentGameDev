using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public GameObject[] SpawnLocations;
    public List<EnemyBase> EnemiesInWave = new List<EnemyBase>();

    [Tooltip("Pool of enemy prefabs this manager can spawn. Each spawn picks one at random, " +
             "the same way a random SpawnLocation is picked.")]
    public GameObject[] EnemyTypes;
    public float SpawnTimer;
    public float SpawnTimerMax;

    public List<int> EnemiesPerWave;

    public int EnemyWave;

    private bool allWavesCleared;

    private void Awake()
    {
        if (EnemyTypes == null || EnemyTypes.Length == 0)
            Debug.LogError($"EnemyManager on '{name}': No EnemyTypes assigned.");

        if (SpawnLocations == null || SpawnLocations.Length == 0)
            Debug.LogError($"EnemyManager on '{name}': No SpawnLocations assigned.");

        if (EnemiesPerWave == null || EnemiesPerWave.Count == 0)
            Debug.LogError($"EnemyManager on '{name}': EnemiesPerWave is empty — no waves configured.");
    }

    // Update is called once per frame
    private void Update()
    {
        if (allWavesCleared) return;

        if (EnemyTypes == null || EnemyTypes.Length == 0 || SpawnLocations == null || SpawnLocations.Length == 0 || EnemiesPerWave == null)
            return;

        if (EnemyWave >= EnemiesPerWave.Count)
        {
            allWavesCleared = true;
            Debug.Log($"EnemyManager on '{name}': All waves cleared.");
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
        }
    }

    private void SpawnEnemy()
    {
        GameObject enemyPrefab = EnemyTypes[Random.Range(0, EnemyTypes.Length)];
        Vector3 spawnPos = SpawnLocations[Random.Range(0, SpawnLocations.Length)].transform.position;

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
    }

    // Called when a tracked enemy's StatsManager fires OnDied. Without this, EnemiesInWave
    // never empties and EnemyWave can never advance past the first wave.
    private void HandleEnemyDied(EnemyBase enemyBase)
    {
        EnemiesInWave.Remove(enemyBase);

        if (enemyBase != null)
            Destroy(enemyBase.gameObject);
    }
}
