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
    public GameObject squadPrefab;
    //[HideInInspector]
    public Queue<GameObject> enemyContainer = new Queue<GameObject>();
    public List<SquadScript> squadSOContainer = new List<SquadScript>();


    public bool chaseTarget = false;
    



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
        Transform squadParent = new GameObject("Squad Parent").transform;
        squadParent.parent = transform;
        GameObject squadGO = Instantiate(squadPrefab, spawnCenter.position, Quaternion.identity, squadParent);
        Transform squadT = squadGO.transform;
        SquadScript squad = squadGO.GetComponent<SquadScript>();

        squad.transform.position = spawnCenter.position;




        // squad.RandomSize();
        squadSOContainer.Add(squad);

        for(int i = 0; i < squadSize; i++)
        {
            Vector3 spawnPos = spawnCenter.position + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0, Random.Range(-spawnRadius, spawnRadius));
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, squadParent);
            squad.squadUnits.Add(enemy);
            enemy.transform.localScale = Vector3.one * squad.scaleSize;
            enemyContainer.Enqueue(enemy);

            enemy.GetComponent<SquadRef>().squad = squad;

            
        }

    }

    void ChaseTarget(bool DoChase)
    {
        foreach (var enemy in enemyContainer)
        {
            if(DoChase)
                enemy.GetComponent<ChaseTarget>().enabled = true;
            else
                enemy.GetComponent<ChaseTarget>().enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {

        _timeToSpawn += Time.deltaTime;
        if (_timeToSpawn >= spawnDelay && isSpawnTime(World.Instance.TimeOfDayInt))
        {
            _timeToSpawn = 0;
            allowSpawning = true;
        }

        if (allowRespawning)
            RespawnDeadEnemies(true);


        if (allowSpawning)
        {
            
            Spawn();
            
            allowSpawning = false;

        }

        //if (Random.Range(1, 10) == 1)
        //    ChaseTarget(chaseTarget);

        
    }

    

   


}
