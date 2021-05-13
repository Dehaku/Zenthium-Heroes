using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public bool allowSpawning = false;
    public Vector3 spawnCenter;
    public float spawnRadius;
    public int maxEnemiesAtOnce;

    public GameObject enemyPrefab;
    //[HideInInspector]
    public GameObject[] enemyContainer;

    // Start is called before the first frame update
    void Start()
    {
        enemyContainer = new GameObject[maxEnemiesAtOnce];
    }

    // Update is called once per frame
    void Update()
    {
        if (World.Instance.TimeOfDay == 725)
        {
            allowSpawning = true;
        }

        if (allowSpawning)
        {
            for (int i = 0; i < maxEnemiesAtOnce; i++)
            {
                Vector3 spawnPos = spawnCenter + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0, Random.Range(-spawnRadius, spawnRadius));
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
                enemyContainer[i] = enemy;
            }
            allowSpawning = false;
            
        }
    }
}
