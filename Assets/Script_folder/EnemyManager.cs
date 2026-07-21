using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public GameObject[] SpawnLocations;
    public List<EnemyBase> EnemiesInWave = new List<EnemyBase>();
    public GameObject Enemy;
    public float SpawnTimer;
    public float SpawnTimerMax;

    public List<int> EnemiesPerWave;

    public int EnemyWave;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SpawnTimer -= Time.deltaTime;
        if (SpawnTimer <= 0)
        {
            if (EnemiesPerWave[EnemyWave] > 0)
            {
                GameObject newEnemy = Instantiate(Enemy, SpawnLocations[Random.Range(0, SpawnLocations.Length)].transform.position, Enemy.transform.rotation);
                EnemiesInWave.Add(newEnemy.GetComponent<EnemyBase>());
                EnemiesPerWave[EnemyWave] -= 1;
                SpawnTimer = SpawnTimerMax;
            }

        }

        if (EnemiesPerWave[EnemyWave] <=0 && EnemiesInWave.Count == 0)
        {
            EnemyWave += 1;
        }
        

        




    }
}
