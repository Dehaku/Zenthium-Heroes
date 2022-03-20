using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class SquadScript : MonoBehaviour
{

    public List<GameObject> squadUnits = new List<GameObject>();
    public List<NavMeshAgent> squadNavs = new List<NavMeshAgent>();
    List<Creature> squadCreatures = new List<Creature>();
    public int squadSize = 5;
    public float scaleSize = 1;
    public int difficulty = 1;
    public float percentDownToBreakFormation = 0.2f;

    public Vector3 squadPosition;
    public Vector3 squadDestination;

    public bool enemySpotted = false;
    public bool breakFormation = false;
    public bool faceInsteadOfChase = true;

    BoxFormation boxFormation;
    RadialFormation radialFormation;

    private void Start()
    {
        boxFormation = GetComponent<BoxFormation>();
        radialFormation = GetComponent<RadialFormation>();
    }

    public void RandomSize()
    {
        scaleSize = Random.Range(0.5f, 1.5f);
    }

    

    public void TargetFound(GameObject target)
    {
        enemySpotted = true;

        foreach (var squaddie in squadUnits)
        {
            if (faceInsteadOfChase)
            {
                // Vector3 direction = (target.transform.position - squaddie.transform.position).normalized;
                // Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                // squaddie.transform.rotation = Quaternion.Slerp(squaddie.transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
            }
            else
            {
                var chaseT = squaddie.GetComponent<ChaseTarget>();
                if (!chaseT)
                    continue;
                chaseT.enabled = true;
                chaseT.target = target;
            }
        }
    }

    void SquaddiesFaceOwnTargets()
    {
        foreach (var squaddie in squadUnits)
        {
            if (!squaddie.GetComponent<Creature>().isConscious)
                continue;


            var squaddieTarget = squaddie.GetComponent<ChaseTarget>().target;
            if(squaddieTarget)
                squaddie.transform.DOLookAt(squaddieTarget.transform.position, 1f, AxisConstraint.Y);
        }
    }

    
    void CacheSquadCreatures()
    {
        squadCreatures.Clear();
        foreach (var squaddie in squadUnits)
        {
            var crea = squaddie.GetComponent<Creature>();
            if (crea)
                squadCreatures.Add(crea);
            else
                Debug.LogWarning("Uh... A squad unit didn't have the Creature/Health system for some reason.");
        }
    }
    void ShouldBreakFormation(float breakPercent)
    {
        // Caching our creature scripts.
        if (squadCreatures.Count != squadUnits.Count)
            CacheSquadCreatures();

        int squadSize = squadUnits.Count;
        int squaddiesConscious = 0;

        foreach (var squaddie in squadCreatures)
        {
            if (squaddie.isConscious)
                squaddiesConscious++;
        }

        int squadBreakpoint = squadSize - (int)(squadSize * percentDownToBreakFormation);

        if(squaddiesConscious < squadBreakpoint)
        {
            breakFormation = true;
        }
        else
        {
            if(breakFormation == true)
                Debug.Log("Reforming formation!");
            breakFormation = false;
            
        }
    }

    float secondTimer = 0f;
    void SecondTimer(float timer)
    {
        secondTimer += Time.deltaTime;
        if (secondTimer >= timer)
        {
            secondTimer = secondTimer % timer;
            SquaddiesFaceOwnTargets();
            ShouldBreakFormation(percentDownToBreakFormation);
        }
    }

    private void Update()
    {
        SecondTimer(1f); // Functions that we only want to run once a second, instead of every frame.
        


        if (!breakFormation)
            SetFormation();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            SetSquadDestination(FindObjectOfType<Player>().transform.position);
        }
    }

    // Tarodev's ExampleArmy modified
    private List<Vector3> _points = new List<Vector3>();
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
    



    public void SetSquadDestination(Vector3 destination)
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

    void CacheNavAgents()
    {
        squadNavs.Clear();
        for (var i = 0; i < squadUnits.Count; i++)
            squadNavs.Add(squadUnits[i].GetComponent<NavMeshAgent>());
    }

    private void SetFormation()
    {
        if (squadNavs.Count != squadUnits.Count)
            if (squadUnits.Count > 0)
                CacheNavAgents();

        

        _points = Formation.EvaluatePoints().ToList();

        var squadPos = transform.position;

        for (var i = 0; i < squadNavs.Count; i++)
        {
            // Safety measure, since some settings don't seem like they'd cause issues, but do.
            if (i >= _points.Count)
            {
                Debug.Log("Bad formation settings, not enough points.");
                break;
            }

            // I tried making smoother checks for this and it just didn't work, so this block is a lil ugly.
            if(squadNavs[i])
            {
                if(squadNavs[i].enabled && squadNavs[i].isActiveAndEnabled)
                {

                    // Make the navAgent actually stop them in formation. The formation stuff can handle noise if we need it.
                    squadNavs[i].stoppingDistance = 0;

                    Vector3 pos = _points[i] + squadPos;
                    squadNavs[i].SetDestination(pos);
                }
            }
        }
    }
}
