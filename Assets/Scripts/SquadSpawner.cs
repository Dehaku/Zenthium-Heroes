using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SquadSpawner : MonoBehaviour
{
    public int spawnHour = 0;
    public int spawnMinute = 0;
    public int squadSize = 5;

    public bool allowSpawning = false;
    public bool allowRespawning = false;
    public Transform spawnCenter;
    public float spawnRadius;
    public int maxEnemiesAtOnce;
    public float spawnDelay;
    float _timeToSpawn;

    public GameObject enemyPrefab;
    //[HideInInspector]
    public Queue<GameObject> enemyContainer = new Queue<GameObject>();
    public List<SquadDefSO> squadSOContainer = new List<SquadDefSO>();



    



    // Start is called before the first frame update
    void Start()
    {
    }

    void RespawnDeadEnemies(bool oneAtATime = false)
    {
        foreach (var enemy in enemyContainer)
        {
            if (enemy.activeSelf == false)
            {
                Vector3 spawnPos = spawnCenter.position + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0, Random.Range(-spawnRadius, spawnRadius));
                enemy.transform.position = spawnPos;
                enemy.SetActive(true);
                if (oneAtATime)
                    return;
            }

        }
    }

    bool isSpawnTime(int TimeOfDay)
    {
        int spawnTime = (spawnHour * 60) + spawnMinute;
        if (TimeOfDay == spawnTime)
            return true;

        return false;
    }

    void Spawn()
    {
        SquadDefSO squad;
        squad = (SquadDefSO)ScriptableObject.CreateInstance(typeof(SquadDefSO));

        squad.RandomSize();
        squadSOContainer.Add(squad);

        for(int i = 0; i < squadSize; i++)
        {
            Vector3 spawnPos = spawnCenter.position + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0, Random.Range(-spawnRadius, spawnRadius));
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
            squad.units.Add(enemy);
            enemy.transform.localScale = Vector3.one * squad.scaleSize;
            enemyContainer.Enqueue(enemy);
        }

    }

    // Update is called once per frame
    void Update()
    {
        _timeToSpawn += Time.deltaTime;
        if (isSpawnTime(World.Instance.TimeOfDay))
        {
            allowSpawning = true;
        }

        if (allowRespawning)
            RespawnDeadEnemies(true);


        if (allowSpawning && _timeToSpawn >= spawnDelay)
        {
            _timeToSpawn = 0;
            Spawn();
            
            allowSpawning = false;

        }


        if (Input.GetKeyDown(KeyCode.H))
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
