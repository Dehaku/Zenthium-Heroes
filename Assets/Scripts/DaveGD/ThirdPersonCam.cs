using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
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

    Vector3 inputMovement;

    //[Header("Events")]
    public event Action<List<GameObject>> onCameraSwitch;



    public enum CameraStyle
    {
        Basic,
        Combat,
        FirstPerson,
        Topdown,
        NewThird
    }

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //SwitchCameraStyle(currentStyle);
        
    }

    private void Start()
    {
        CycleCameraStyles();
    }

    public void CycleCameraInput(InputAction.CallbackContext context)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (context.started)
            CycleCameraStyles();
    }

    public int currentCam = -1;
    int amountOfCameras = 3;
    public void CycleCameraStyles()
    {
        currentCam++;
        if (currentCam >= amountOfCameras)
            currentCam = 0;

        if (currentCam == 0)
            SwitchCameraStyle(CameraStyle.Basic);
        if (currentCam == 1)
            SwitchCameraStyle(CameraStyle.Combat);
        if (currentCam == 2)
            SwitchCameraStyle(CameraStyle.FirstPerson);
    }

    void Inputs()
    {
       
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
            Vector3 inputDir = orientation.forward * inputMovement.y + orientation.right * inputMovement.x;

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
        if (newStyle == CameraStyle.FirstPerson) 
        {
            firstPersonCam.SetActive(true);
            baseFOV = firstPersonCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
            var pointAimers = firstPersonCam.GetComponentsInChildren<TagPointAimer>();
            foreach (var item in pointAimers)
                go.Add(item.gameObject);
        }
        if (newStyle == CameraStyle.Topdown)
        {
            topDownCam.SetActive(true);
            baseFOV = topDownCam.GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.FieldOfView;
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

    #region inputfunctions

    public void Movement(InputAction.CallbackContext context)
    {
        var inputValue = context.ReadValue<Vector2>();
        inputMovement = inputValue;
    }

    #endregion

}
