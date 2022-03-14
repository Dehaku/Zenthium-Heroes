using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SquadTargetAssigner : MonoBehaviour
{
    public float targetAssignerRate = 5;
    float _targetAssignerTrack = 0;
    
    public List<SquadScript> squadContainer;
    public List<GameObject> villainTargets;



    private void Awake()
    {
        squadContainer = GetComponent<SquadSpawner>().squadSOContainer;
    }

    void GetVillianTargets()
    {
        //var timer = Time.realtimeSinceStartupAsDouble;
        //Debug.Log("T:" + (Time.realtimeSinceStartupAsDouble - timer) + ":" + villianTargets.Count);
        villainTargets.Clear();

        var targets = FindObjectsOfType<VillainTarget>();
        foreach (var tar in targets)
        {
            if(tar.IsValidTarget())
                villainTargets.Add(tar.gameObject);
        }

        
    }
    void AssignSquadTargets()
    {
        foreach (var squad in squadContainer)
        {
            var squadPos = squad.transform.position;
            // TODO: This... might be wrong? I don't understand the command very well. It might be using the this scripts's rigidbody, instead of the squad's body.
            var tarPos = villainTargets.OrderBy(rigidbody => Vector3.Distance(rigidbody.transform.position, squadPos)).First();
            squad.SetSquadDestination(tarPos.transform.position);
        }
    }

    private void Update()
    {
        _targetAssignerTrack += Time.deltaTime;
        if(_targetAssignerTrack >= targetAssignerRate)
        {
            _targetAssignerTrack = 0;
            GetVillianTargets();
            AssignSquadTargets();
        }
    }



}
