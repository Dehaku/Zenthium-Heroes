using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
//brought to you by M dot Strange

public class AgentJumpToTarget : MonoBehaviour
{   
    public NavMeshAgent NavMeshAgent;
    public Rigidbody Rigidbody;
    public GameObject Target;
    public float ReachedStartPointDistance = 0.5f;
    public Transform DummyAgent;
    public Vector3 EndJumpPosition;
    public float MaxJumpableDistance = 80f;
    public float JumpTime = 0.6f;
    public float AddToJumpHeight;

    Transform _dummyAgent;
    public Vector3 JumpStartPoint;
    Vector3 JumpMidPoint;
    Vector3 JumpEndPoint;
    bool checkForStartPointReached;
    Transform _transform;
    List<Vector3> Path = new List<Vector3>();
    float JumpDistance;
    Vector3[] _jumpPath;
    bool previousRigidBodyState;


    public void GetStartPointAndMoveToPosition()
    {
        JumpStartPoint = GetJumpStartPoint();
        MoveToStartPoint();
    }

    public void PerformJump()
    {
        SpawnAgentAndGetPoint();
    }

    private void OnEnable()
    {
        checkForStartPointReached = false;
        _transform = transform;
    }

    Vector3 GetJumpStartPoint()
    {
        NavMeshPath hostAgentPath = new NavMeshPath();
        NavMeshAgent.CalculatePath(Target.transform.position, hostAgentPath);
        var endPointIndex = hostAgentPath.corners.Length - 1;
        return hostAgentPath.corners[endPointIndex];

        //Improvement to make- get the jump distance using the start and end point
        // use that to set the Jump Time
    }

    void MoveToStartPoint()
    {
        checkForStartPointReached = true;
        NavMeshAgent.isStopped = false;
        NavMeshAgent.SetDestination(JumpStartPoint);
    }

    void ReadyToJump()
    {
        //Do your pre_jump animation
    }

    void SpawnAgentAndGetPoint()
    {
        // If using Pooling Spawn here instead
        _dummyAgent = Instantiate(DummyAgent, Target.transform.position, Quaternion.identity);
        var info = _dummyAgent.GetComponent<ReturnNavmeshInfo>();
        EndJumpPosition = info.ReturnClosestPointBackToAgent(transform.position);
        JumpEndPoint = EndJumpPosition;

        MakeJumpPath();

    }

    void MakeJumpPath()
    {
        Path.Add(JumpStartPoint);

        var tempMid = Vector3.Lerp(JumpStartPoint, JumpEndPoint, 0.5f);
        tempMid.y = tempMid.y + NavMeshAgent.height + AddToJumpHeight;

        Path.Add(tempMid);

        Path.Add(JumpEndPoint);

        JumpDistance = Vector3.Distance(JumpStartPoint, JumpEndPoint);

        if (JumpDistance <= MaxJumpableDistance)
        {
            DoJump();
        }
        else
        {
            Debug.Log("Too far to jump");
        }
    }

    void DoJump()
    {
        previousRigidBodyState = Rigidbody.isKinematic;
        NavMeshAgent.enabled = false;
        Rigidbody.isKinematic = true;

        _jumpPath = Path.ToArray();

        // if you don't want to use a RigidBody change this to
        //transform.DoLocalPath per the DoTween doc's
        Rigidbody.DOLocalPath(_jumpPath, JumpTime, PathType.CatmullRom).OnComplete(JumpFinished);
    }

    void JumpFinished()
    {
        NavMeshAgent.enabled = true;
        Rigidbody.isKinematic = previousRigidBodyState;

        // If using Pooling DeSpawn here instead
        Destroy(_dummyAgent.gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            GetStartPointAndMoveToPosition();

        if (Input.GetKeyDown(KeyCode.X))
            PerformJump();

        if (checkForStartPointReached)
        {
            var distance = (_transform.position - JumpStartPoint).sqrMagnitude;

            if (distance <= ReachedStartPointDistance * ReachedStartPointDistance)
            {
                ReadyToJump();

                if(NavMeshAgent.isOnNavMesh)
                {
                    NavMeshAgent.isStopped = true;
                }
               
                checkForStartPointReached = false;               
            }           
        }
    }
}
