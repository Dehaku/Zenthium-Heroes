using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaPrefabSpawner : MonoBehaviour
{
    public GameObject[] prefabs;
    public int spawnAmount;
    public Vector3 centerPos;
    public Vector3 spawnArea;
    [Tooltip("Each prefab spawned will have it's scales randomly modified by the following scale.")]
    public Vector3 RandomScale = new Vector3(0.25f, 0.25f, 0.25f);

    public bool spawnStuff = false;


    // Start is called before the first frame update
    void Start()
    {
        if (prefabs[0] == null)
            Debug.LogError("NatureSpawner has no prefabs assigned.");
    }

    // Update is called once per frame
    void Update()
    {
        if(spawnStuff)
        {
            spawnStuff = false;
            SpawnObjects(spawnAmount);
        }
    }

    public void SpawnObjects(int amount)
    {
        int successfulSpawns = 0;

        for(int i = 0; i < amount; i++)
        {
            Vector3 rayStartPos = centerPos;

            rayStartPos.x += Random.Range(-spawnArea.x, spawnArea.x);
            rayStartPos.y += Random.Range(-spawnArea.y, spawnArea.y);
            rayStartPos.z += Random.Range(-spawnArea.z, spawnArea.z);

            

            Vector3 startP = rayStartPos;
            Vector3 destP = startP + Vector3.down;
            Vector3 direction = destP - startP;

            Ray ray = new Ray(startP, direction);

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000)) { continue; }

            if (hit.collider.gameObject.GetComponent<ChunkTag>() == null)
                continue;

            int prefabNum = Random.Range(0, prefabs.Length - 1);
            GameObject obj = Instantiate(prefabs[prefabNum], hit.point, Quaternion.identity);

            

            obj.transform.localScale = new Vector3(obj.transform.localScale.x + Random.Range(-RandomScale.x, RandomScale.x), 
                obj.transform.localScale.y + Random.Range(-RandomScale.y, RandomScale.y), 
                obj.transform.localScale.z + Random.Range(-RandomScale.z, RandomScale.z));

            obj.transform.Rotate(new Vector3(0, Random.Range(0, 360), 0));

            successfulSpawns++;
        }

    }
}
