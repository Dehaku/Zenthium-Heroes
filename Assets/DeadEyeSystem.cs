using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Thank you, Omar.  https://www.youtube.com/watch?v=LofJMnWPClo

public class DeadEyeSystem : MonoBehaviour
{
    
    public enum DeadEyeState
    {
        off, 
        aiming, 
        shooting
    };

    public float deadEyeAimSpeed = 30f;
    public DeadEyeState deadEyeState = DeadEyeState.off;

    public List<Transform> targets;
    public Transform[] targetCross;
    Camera cam;
    //public CameraLook cam_look;
    public ThirdPersonCam cam_look;

    public float cooldownTime = 0.5f;
    private float _cooldownTimer = 0.0f;

    public float deadEyeTimescale = 0.3f;
    public bool keepSlowmoWhileShooting = false;
    float fixedDeltaTimeDefault;

    public float aimErrorAutoSnapAim = 3f;
    public float aimErrorAllowance = 0.1f;

    public Volume ppv;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        fixedDeltaTimeDefault = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (_cooldownTimer > 0.0f)
            _cooldownTimer -= Time.deltaTime;
        else
        {
            _cooldownTimer = 0.0f;
        }

        // Hold Deadeye to enter Dead Eye Mode
        if (Input.GetKey(KeyCode.G))
        {
            // Dynamic camera system, hopefully this helps that. Might be redundant with cinemachine?
            cam = Camera.main;

            if (deadEyeState == DeadEyeState.off)
                deadEyeState = DeadEyeState.aiming;
        }

        // Trigger while in Dead Eye Mode, assaign target, else shoot
        if(Input.GetKeyDown(KeyCode.F))
        {
            if (deadEyeState == DeadEyeState.off)
                Fire();
            if(deadEyeState == DeadEyeState.aiming)
            {
                // Assign target
                RaycastHit hit;
                if(Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, 100))
                {
                    GameObject tmpTarget = new GameObject();
                    tmpTarget.transform.position = hit.point;
                    tmpTarget.transform.parent = hit.transform;
                    targets.Add(tmpTarget.transform);
                }
            }
        }
        // Release Deadeye, enter shooting state then exit
        if (Input.GetKeyUp(KeyCode.G))
        {
            if (deadEyeState == DeadEyeState.aiming)
                deadEyeState = DeadEyeState.shooting;
        }

    }

    private void FixedUpdate()
    {
        UpdateState();
        UpdateTargetsUI();
    }

    private void UpdateTargetsUI()
    {
        for(int i = 0; i < targetCross.Length; i++)
        {
            if (i < targets.Count)
            {
                targetCross[i].gameObject.SetActive(true);
                targetCross[i].position = Camera.main.WorldToScreenPoint(targets[i].position);
            }
                
            else
                targetCross[i].gameObject.SetActive(false);
        }
    }

    void UpdateState()
    {
        if(deadEyeState == DeadEyeState.off)
        {
            // Enable Player Camera Control
            //cam_look.SetActiveCameraInputs(true);
            if (ppv.weight > 0.0f)
                ppv.weight -= Time.deltaTime * 2;

        }
        else if(deadEyeState == DeadEyeState.aiming)
        {
            // Enable Player Camera Control
            //cam_look.SetActiveCameraInputs(true);
            Time.timeScale = deadEyeTimescale;
            Time.fixedDeltaTime = Time.timeScale * fixedDeltaTimeDefault;
            if (ppv.weight < 1.0f)
                ppv.weight += Time.deltaTime * 2;
        }
        else
        {
            // DISABLE player camera control, camera is automated.
            cam_look.SetActiveCameraInputs(false);
            if(keepSlowmoWhileShooting)
            {
                Time.timeScale = deadEyeTimescale;
            }
            else
            {
                Time.timeScale = 1f;
            }
            
            Time.fixedDeltaTime = Time.timeScale * fixedDeltaTimeDefault;
            UpdateTargets();
        }
    }

    void UpdateTargets()
    {
        if(deadEyeState == DeadEyeState.shooting && targets.Count > 0)
        {
            

            Transform currTarget = targets[0];
            Quaternion rot = Quaternion.LookRotation(currTarget.position - cam.transform.position);
            // works cam_look.LookAt(currTarget.position);
            float diff = (cam.transform.eulerAngles - rot.eulerAngles).magnitude;
            if (diff <= aimErrorAutoSnapAim)
            {
                //cam_look.LookAt(currTarget.position);
                cam_look.LookAt(Quaternion.Slerp(cam.transform.rotation, rot, 1));
            }
            else
                cam_look.LookAt(Quaternion.Slerp(cam.transform.rotation, rot, deadEyeAimSpeed * Time.deltaTime));
            
            
            diff = (cam.transform.eulerAngles - rot.eulerAngles).magnitude;
            if(diff <= aimErrorAllowance && _cooldownTimer <= 0.0f)
            {
                Debug.Log("Diff:" + diff);
                Fire();
                targets.Remove(currTarget);
                Destroy(currTarget.gameObject);
            }

            if(targets.Count == 0)
            {
                EndDeadEye();
            }
        }
    }

    void Fire()
    {
        print("Fire!");
        _cooldownTimer = cooldownTime;
    }

    void EndDeadEye()
    {
        deadEyeState = DeadEyeState.off;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = Time.timeScale * fixedDeltaTimeDefault;
        cam_look.SetActiveCameraInputs(true);
    }
}
