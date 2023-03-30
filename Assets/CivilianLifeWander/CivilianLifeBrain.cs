using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CivilianLifeBrain : MonoBehaviour
{

    public List<Interactable> routine;
    public Interactable currentTarget;
    public float approachDistanceThreshold = 0.1f;
    public float reserveDistance = 5f;
    float _interactDuration = -1;
    float _interactTimer = 0;
    

    [Header("References")]
    public NavMeshAgent navAgent;



    // Start is called before the first frame update
    void Start()
    {
        if (!navAgent)
            navAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!navAgent)
            return;
        if (routine.Count == 0)
            return;

        if (currentTarget == null)
            GetNewTarget();

        ApproachTarget();

        if(navAgent.stoppingDistance > 0)
            Debug.DrawLine(transform.position, currentTarget.interactPosAndRot.position, Color.red);
        else if(_interactTimer > 0)
            Debug.DrawLine(transform.position, currentTarget.interactPosAndRot.position, Color.green);
        else
            Debug.DrawLine(transform.position, currentTarget.interactPosAndRot.position,Color.white);

    }

    //float _reservedWaitSpeedBuffer = 0;
    void ApproachTarget()
    {
        if (currentTarget == null)
        {
            GetNewTarget();
            return;
        }

        // Reserving the object, if it's available.
        if (currentTarget.reserved == null && navAgent.remainingDistance < reserveDistance)
            currentTarget.reserved = this;

        if(navAgent.destination != currentTarget.interactPosAndRot.position)
        {
            //Debug.Log(name + " destination mismatch, resetting destination");
            //Debug.Log(name + " Dest: " + navAgent.destination + ":" + currentTarget.interactPosAndRot.position);
            navAgent.destination = currentTarget.interactPosAndRot.position;
        }

        // Check if we should wait.
        if (currentTarget.reserved != this && navAgent.remainingDistance < (reserveDistance / 2))
            navAgent.stoppingDistance = 100;
        else
            navAgent.stoppingDistance = 0;
        /*
        {
            _reservedWaitSpeedBuffer = navAgent.speed;
            navAgent.speed = 0;
        }
        else
        {
            // No longer waiting.
            if (_reservedWaitSpeedBuffer != 0)
            {
                navAgent.speed = _reservedWaitSpeedBuffer;
                _reservedWaitSpeedBuffer = 0;
            }
        }
        */

        
            


        // Make sure our path is valid before checking distance.
        if(!navAgent.pathPending && navAgent.remainingDistance <= approachDistanceThreshold)
        {
            Debug.Log(name + "remaining distance: " + navAgent.remainingDistance);
            if(currentTarget.reserved == this)
                InteractWithTarget();
        }

        
            
    }

    public void InteractWithTarget()
    {
        _interactTimer -= Time.deltaTime;

        // Start Interaction
        Debug.Log(name + " interacts with " + currentTarget.myObject.name);
        
        // Timers
        if(_interactDuration == -1)
        {
            _interactDuration = currentTarget.interactDuration;
            _interactTimer = _interactDuration;
        }
        // Maintain Interaction

        // **PlaySound/Animations**







        // Completed Interaction
        if (_interactTimer <= 0)
        {
            _interactDuration = -1;
            CompleteInteraction();
        }

    }

    void CompleteInteraction()
    {
        if (currentTarget.reserved = this)
            currentTarget.reserved = null;
        GetNewTarget();
    }

    void GetNewTarget()
    {
        Debug.Log(name + " is getting new target");
        // If no target or only one possible target, set target to first position.
        if (currentTarget == null || routine.Count == 1)
            currentTarget = routine[0];
        else
        {
            for(int i = 0; i < routine.Count; i++)
            {
                if(routine[i] == currentTarget)
                {
                    Debug.Log(name + " " + i);
                    // Make sure we're not going to overflow, if we are, set target to first in routine. Otherwise, set to next in routine.
                    if (i == routine.Count - 1)
                    {
                        Debug.Log(name + " loop");
                        currentTarget = routine[0];
                        break;
                    }
                        
                    else
                    {
                        currentTarget = routine[i + 1];
                        break;
                    }
                        
                }
            }
        }

        if(currentTarget)
            Debug.Log(name + " chose " + currentTarget.name);
        else
            Debug.Log(name + " has nothing to choose. ");
    }
}
