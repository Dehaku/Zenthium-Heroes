using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PossessorController : MonoBehaviour
{
    public bool allowPossessing = true;
    public bool allowCompleteUnPossessing = true;

    public FreeCam freeCam;
    public Cinemachine.CinemachineFreeLook cam;
    public Cinemachine.CinemachineVirtualCamera vcam;
    public PlayerPossessable currentlyPossessing;

    RaycastHit m_HitInfo = new RaycastHit();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(allowPossessing)
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(2))
                AttemptPossess();

        if(allowCompleteUnPossessing)
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(2))
                if (currentlyPossessing)
                {
                    currentlyPossessing.UnPossess();
                    currentlyPossessing = null;
                }
                    
    }

    void AttemptPossess()
    {
        // Casting a ray to find something to possess
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray.origin, ray.direction, out m_HitInfo))
        {

            Debug.Log("Possess Hit: " + m_HitInfo.collider.name);
            var possessable = m_HitInfo.collider.GetComponentInChildren<PlayerPossessable>();
            // Making sure our hit is possessable.
            if(possessable)
            {

                // Don't try to repossess current target.
                if (possessable == currentlyPossessing)
                    return;

                // Some targets may need work to possess
                Debug.Log("Attempting Possession!");
                bool successfulPossess = possessable.Possess();

                if(successfulPossess)
                {
                    // Cleaning up old possession and giving their agency back
                    if (currentlyPossessing)
                        currentlyPossessing.UnPossess();

                    // Tracking our current possession
                    currentlyPossessing = possessable;

                    if(cam)
                    {
                        cam.Follow = possessable.transform;
                        cam.LookAt = possessable.transform;
                    }
                    if(vcam)
                    {
                        vcam.Follow = possessable.transform;
                        vcam.LookAt = possessable.transform;
                    }
                    if(freeCam)
                    {
                        freeCam.enabled = false;
                    }
                    
                }
            }
        }
    }
}
