using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class SquadScript : MonoBehaviour
{

    public List<GameObject> squadUnits = new List<GameObject>();
    public int squadSize = 5;
    public float scaleSize = 1;
    public int difficulty = 1;

    public Vector3 squadPosition;
    public Vector3 squadDestination;

    public bool enemySpotted = false;


    public void RandomSize()
    {
        scaleSize = Random.Range(0.5f, 1.5f);
    }


    private void Update()
    {
        if (!enemySpotted)
            SetFormation();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            SetSquadDestination(FindObjectOfType<Player>().transform.position);
        }
    }

    // Tarodev's ExampleArmy modified

    private FormationBase _formation;

    public FormationBase Formation
    {
        get
        {
            if (_formation == null) _formation = GetComponent<FormationBase>();
            return _formation;
        }
        set => _formation = value;
    }



    private List<Vector3> _points = new List<Vector3>();

    private void Awake()
    {

    }


    void SetSquadDestination(Vector3 destination)
    {
        if (squadUnits.Count < 1)
        {
            Debug.Log("There's no units in a pathing squad.");
            return;
        }

        // Squad navAgent takes on Units navAgent speed properties.
        NavMeshAgent navAgent = GetComponent<NavMeshAgent>();
        NavMeshAgent unitAgent = squadUnits[0].GetComponent<NavMeshAgent>();
        navAgent.speed = (unitAgent.speed * 0.9f); // A little slower, so they can easily hold formation.
        navAgent.angularSpeed = unitAgent.angularSpeed;
        navAgent.acceleration = unitAgent.acceleration;


        if(navAgent.isOnNavMesh)
            navAgent.SetDestination(destination);
        else
        {
            Debug.Log("Not on Navmesh, trying to fix.");
            navAgent.enabled = false;
            navAgent.enabled = true;
            navAgent.SetDestination(destination);
            
        }
    }

    private void SetFormation()
    {
        _points = Formation.EvaluatePoints().ToList();

        var squadPos = transform.position;

        for (var i = 0; i < squadUnits.Count; i++)
        {
            // Safety measure, since some settings don't seem like they'd cause issues, but do.
            if (i >= _points.Count)
            {
                Debug.Log("Bad formation settings, not enough points.");
                break;
            }
                

            var navAgent = squadUnits[i].GetComponent<NavMeshAgent>();
            // Make the navAgent actually stop them in formation. The formation stuff can handle noise if we need it.
            navAgent.stoppingDistance = 0;

            Vector3 pos = _points[i] + squadPos;
            navAgent.SetDestination(pos);
        }
    }

}
