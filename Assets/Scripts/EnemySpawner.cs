using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public bool allowSpawning = false;
    public bool allowRespawning = true;
    public Vector3 spawnCenter;
    public float spawnRadius;
    public int maxEnemiesAtOnce;
    public float spawnDelay;
    float _timeToSpawn;

    public GameObject enemyPrefab;
    //[HideInInspector]
    public Queue<GameObject> enemyContainer = new Queue<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
    }

    void RespawnDeadEnemies(bool oneAtATime = false)
    {
        foreach (var enemy in enemyContainer)
        {
            if(enemy.activeSelf == false)
            {
                Vector3 spawnPos = spawnCenter + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0, Random.Range(-spawnRadius, spawnRadius));
                enemy.transform.position = spawnPos;
                enemy.SetActive(true);
                if(oneAtATime)
                    return;
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        _timeToSpawn += Time.deltaTime;
        if (World.Instance.TimeOfDay == 725)
        {
            allowSpawning = true;
        }

        if (allowRespawning)
            RespawnDeadEnemies(true);

        
        if (allowSpawning && _timeToSpawn >= spawnDelay)
        {
            _timeToSpawn = 0;
            for (int i = 0; i < maxEnemiesAtOnce; i++)
            {
                Vector3 spawnPos = spawnCenter + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0, Random.Range(-spawnRadius, spawnRadius));
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
                //enemyContainer[i] = enemy;
                enemyContainer.Enqueue(enemy);
            }
            allowSpawning = false;
            
        }


        if(Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("EnemyContainer: " + enemyContainer.Count);

            foreach (var enemy in enemyContainer)
            {
                
                
                if (Random.Range(0, 2) == 1)
                    enemy.SetActive(false);
            }
        }
    }
}
