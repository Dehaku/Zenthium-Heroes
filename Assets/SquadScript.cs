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



    private void SetFormation()
    {
        _points = Formation.EvaluatePoints().ToList();

        

        for (var i = 0; i < squadUnits.Count; i++)
        {
            if (i >= _points.Count)
                break;
            var navAgent = squadUnits[i].GetComponent<NavMeshAgent>();
            navAgent.stoppingDistance = 0;

            Vector3 pos = _points[i] + squadPosition;
            navAgent.SetDestination(pos);
            

            Debug.DrawLine(pos, pos + (Vector3.up * 5));
            //squadUnits[i].transform.position = Vector3.MoveTowards(squadUnits[i].transform.position, transform.position + _points[i], _unitSpeed * Time.deltaTime);
        }
    }

}
