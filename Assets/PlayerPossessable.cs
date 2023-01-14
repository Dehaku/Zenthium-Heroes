using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerPossessable : MonoBehaviour
{
    public bool allowPossessing = true;

    public PhysicsBasedCharacterController.CharacterManager characterManager;
    public Rigidbody rb;
    public NavMeshAgent navAgent;
    public ClickToMove clickToMove;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    

    public void UnPossess()
    {
        if(characterManager)
            characterManager.enabled = false;
        if(rb)
            rb.isKinematic = true;
        if(navAgent)
            navAgent.enabled = true;
    }

    public bool Possess()
    {
        // Make sure this is allowed to be possessed at this time.
        if (!allowPossessing)
            return false;

        // Null Checks since more objects may be added that don't include others.
        if(characterManager)
            characterManager.enabled = true;
        if(rb)
            rb.isKinematic = false;
        if(navAgent)
            navAgent.enabled = false;
        if(clickToMove)
            clickToMove.enabled = false;

        return true;
    }
}
