using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform camHolder;
    public Transform player;
    public Transform playerObj;
    public Rigidbody rb;

    public float rotationSpeed;

    public Transform combatLookAt;

    public GameObject thirdPersonCam;
    public GameObject combatCam;
    [HideInInspector]public GameObject topDownCam;
    public GameObject firstPersonCam;
    [HideInInspector] public GameObject newThirdCam;

    public float baseFOV;

    public CameraStyle currentStyle;

    //[Header("Events")]
    public event Action<List<GameObject>> onCameraSwitch;



    public enum CameraStyle
    {
        Basic,
        Combat,
        Topdown,
        FirstPerson,
        NewThird
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SwitchCameraStyle(currentStyle);
    }

    void Inputs()
    {
        // switch styles
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCameraStyle(CameraStyle.Basic);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCameraStyle(CameraStyle.Combat);
        //if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCameraStyle(CameraStyle.Topdown);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCameraStyle(CameraStyle.FirstPerson);
        //if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchCameraStyle(CameraStyle.NewThird);
    }

    private void FixedUpdate()
    {
        


    }

    private void Update()
    {
        Inputs();

        // rotate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        
        if(viewDir.normalized != Vector3.zero)
            orientation.forward = viewDir.normalized;

        // roate player object
        if (currentStyle == CameraStyle.Basic || currentStyle == CameraStyle.Topdown)
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (inputDir != Vector3.zero)
                playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
        }

        else if (currentStyle == CameraStyle.NewThird)
        {
            Vector3 dirToCombatLookAt = combatLookAt.position - new Vector3(transform.position.x, combatLookAt.position.y, transform.position.z);
            orientation.forward = dirToCombatLookAt.normalized;

            playerObj.forward = dirToCombatLookAt.normalized;
        }

        else if (currentStyle == CameraStyle.FirstPerson || currentStyle == CameraStyle.Combat)
        {
            Vector3 flattenedAngle = Camera.main.transform.forward;
            flattenedAngle.y = 0; // Removing the up/down facing.

            playerObj.forward = flattenedAngle;
            orientation.forward = flattenedAngle;
        }
    }

    private void SwitchCameraStyle(CameraStyle newStyle)
    {
        combatCam.SetActive(false);
        thirdPersonCam.SetActive(false);
        topDownCam.SetActive(false);
        firstPersonCam.SetActive(false);
        newThirdCam.SetActive(false);

        List<GameObject> go = new List<GameObject>();

        if (newStyle == CameraStyle.Basic)
        {
            thirdPersonCam.SetActive(true);
            baseFOV = thirdPersonCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
            var pointAimers = thirdPersonCam.GetComponentsInChildren<TagPointAimer>();
            foreach (var item in pointAimers)
                go.Add(item.gameObject);
            
        }
        if (newStyle == CameraStyle.Combat) 
        {
            combatCam.SetActive(true);
            baseFOV = combatCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
            var pointAimers = combatCam.GetComponentsInChildren<TagPointAimer>();
            foreach (var item in pointAimers)
                go.Add(item.gameObject);
        }
        if (newStyle == CameraStyle.Topdown) 
        {
            topDownCam.SetActive(true);
            baseFOV = topDownCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
        }
        if (newStyle == CameraStyle.FirstPerson) 
        {
            firstPersonCam.SetActive(true);
            baseFOV = firstPersonCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
            var pointAimers = firstPersonCam.GetComponentsInChildren<TagPointAimer>();
            foreach (var item in pointAimers)
                go.Add(item.gameObject);
        }
        if (newStyle == CameraStyle.NewThird) 
        {
            newThirdCam.SetActive(true);
            baseFOV = newThirdCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
        }

        currentStyle = newStyle;

        if (onCameraSwitch != null)
            onCameraSwitch(go);
    }

    public void DoFov(float endValue)
    {
        //GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
        var freeCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<Cinemachine.CinemachineFreeLook>();
        if (freeCam)
        {
            DOVirtual.Float(freeCam.m_Lens.FieldOfView, endValue, 0.25f, v => freeCam.m_Lens.FieldOfView = v);
        }
        else
        {
            var virtCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            if (virtCam)
            {
                DOVirtual.Float(virtCam.m_Lens.FieldOfView, endValue, 0.25f, v => virtCam.m_Lens.FieldOfView = v);
            }
            else
            {
                Debug.LogError("No virt or free cam detected!");
            }
        }
    }

    public void DoTilt(float zTilt)
    {
        // This ended up way bigger than I expected.
        var freeCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<Cinemachine.CinemachineFreeLook>();
        if(freeCam)
        {
            DOVirtual.Float(0, zTilt, 0.25f, v => freeCam.m_Lens.Dutch = v);
        }
        else
        {
            var virtCam = GetComponent<Cinemachine.CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            if(virtCam)
            {
                DOVirtual.Float(0, zTilt, 0.25f, v => virtCam.m_Lens.Dutch = v);
            }
            else
            {
                Debug.LogError("No virt or free cam detected!");
            }
        }
    }
}
