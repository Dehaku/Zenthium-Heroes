using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class PlayerAimController : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private CinemachineInputProvider inputProvider;

    public GameObject controller;
    public ThirdPersonMovement thirdPersonController;

    public GameObject aimCamera;
    public Transform aimLookAt;
    public Vector3 aimLookAtOffset;
    public GameObject aimReticle;
    public GameObject aimReticle2;
    public float defaultSensitivity = 1;
    public float defaultZoomedSensitivity = 1;
    float sensitivity = 1;
    public float defaultStickSensitivity = 1;
    public float defaultStickZoomedSensitivity = 1;
    float stickSensitivity = 1;

    public AxisState xAxis;
    public AxisState yAxis;
    public Vector2 aimInput;

    public float camMainMaxZoom = 20;

    public float camMainDistance = 10f;
    public float camShoulderDistance = 4.5f;

    public Vector3 camMainOffset = new Vector3(1.5f, 0, 0);
    public Vector3 camShoulderOffset = new Vector3(1.5f, 0, 0);

    public float camMainFOV = 40;
    public float camShoulderFOV = 20;

    Cinemachine3rdPersonFollow aim;
    CinemachineVirtualCamera aimSettings;

    public LayerMask aimLayer;

    // Start is called before the first frame update
    void Start()
    {
        aimSettings = aimCamera.GetComponent<CinemachineVirtualCamera>();
        aim = aimSettings.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    }

    void MainCam()
    {
        aimReticle.SetActive(false);
        aimReticle2.SetActive(false);
        aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camMainDistance, Time.deltaTime * 4);
        aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camMainOffset, Time.deltaTime * 4);
        aimSettings.m_Lens.FieldOfView = Mathf.Lerp(aimSettings.m_Lens.FieldOfView, camMainFOV, Time.deltaTime * 4);

        xAxis.m_MaxSpeed = 500;
        yAxis.m_MaxSpeed = 300;

        var mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll < 0)
            camMainDistance += 3;
        if (mouseScroll > 0)
            camMainDistance -= 3;

        camMainDistance = Mathf.Clamp(camMainDistance, camShoulderDistance, camMainMaxZoom);

        sensitivity = defaultSensitivity;
        stickSensitivity = defaultStickSensitivity;
    }

    void ShoulderCam()
    {
        aimReticle.SetActive(true);
        aimReticle2.SetActive(true);
        aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camShoulderDistance, Time.deltaTime * 4);
        aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camShoulderOffset, Time.deltaTime * 4);
        aimSettings.m_Lens.FieldOfView = Mathf.Lerp(aimSettings.m_Lens.FieldOfView, camShoulderFOV, Time.deltaTime * 4);

        xAxis.m_MaxSpeed = 100;
        yAxis.m_MaxSpeed = 100;

        var mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll < 0)
            camShoulderFOV += 3;
        if (mouseScroll > 0)
            camShoulderFOV -= 3;

        camShoulderFOV = Mathf.Clamp(camShoulderFOV, 10, camMainFOV);

        sensitivity = defaultZoomedSensitivity;
        stickSensitivity = defaultStickZoomedSensitivity;
    }


    private void FixedUpdate()
    {
        // Getting the device to work around a mouse delta bug.
        string Device = "";
        if(playerInput.actions["Look"].activeControl != null)
            if (playerInput.actions["Look"].activeControl.device != null)
                Device = playerInput.actions["Look"].activeControl.device.displayName;
        




        Vector2 inputValue = new Vector2();
        if (Device == "Mouse")
        {
            //I have no idea what's wrong with the mouse delta, I just use the old input for mouse down in Update().
            
        }
        else
            inputValue += playerInput.actions["Look"].ReadValue<Vector2>() * stickSensitivity;

        aimInput += inputValue;
        aimInput.y = Mathf.Clamp(aimInput.y, -88, 88);
    }

    // Update is called once per frame
    void Update()
    {
        // Always had problems if the camera follow target was a child of the player, this is a work around.
        aimLookAt.transform.position = transform.position + aimLookAtOffset;

        if(Cursor.lockState != CursorLockMode.Locked)
        { return;  }

        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);

        aimInput.x += Input.GetAxisRaw("Mouse X") * sensitivity;
        aimInput.y += -Input.GetAxisRaw("Mouse Y") * sensitivity;

        //aimLookAt.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, 0);
        //aimLookAt.eulerAngles = new Vector3(aimInput.x, aimInput.y, 0);
        aimLookAt.eulerAngles = new Vector3(aimInput.y, aimInput.x, 0);

        AimReticleAdjust();

        if (Input.GetMouseButton(1))
        {
            thirdPersonController.SetRotateOnMove(false);

            Ray camRay = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
            if (!Physics.Raycast(camRay, out RaycastHit camHit, Mathf.Infinity)) { }

            Vector3 worldAimTarget = camHit.point;
            worldAimTarget.y = controller.transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = aimDirection; // Vector3.Lerp(controller.transform.forward, aimDirection, Time.deltaTime * 20f);
            
            //var rotation = Quaternion.LookRotation(controller.transform.position + Camera.main.transform.forward);
            //controller.transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        }
        else if (Input.GetMouseButtonUp(1))
        {// Fix our camera when we're not over the shouldering.
            thirdPersonController.SetRotateOnMove(true);
            //controller.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        if (Input.GetMouseButton(1))
            ShoulderCam();
        else
            MainCam();
        
    }

    private void AimReticleAdjust()
    {
        Vector3 center = aimLookAt.transform.position;
        Ray camRay = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
        if (!Physics.Raycast(camRay, out RaycastHit camHit, Mathf.Infinity)) { }

        Ray ray = new Ray(center, (camHit.point - center).normalized * 100);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, aimLayer)) { }

        aimReticle.transform.position = Camera.main.WorldToScreenPoint(hit.point);
    }

    public void Look(InputAction.CallbackContext context)
    {
        // var inputValue = context.ReadValue<Vector2>();
        // inputValue.x = Mathf.Clamp(inputValue.x, -5, 5);
        // inputValue.y = Mathf.Clamp(inputValue.y, -5, 5);
        // 
        // 
        // Debug.Log("Map:" + context.action.actionMap);
        // 
        //     Debug.Log("context:" + context.ReadValue<Vector2>()
        //     + ", aimInput: " + aimInput
        //     );
        // aimInput += inputValue;
        // aimInput.x = Mathf.Clamp(aimInput.x, -180, 180);
        // aimInput.y = Mathf.Clamp(aimInput.y, -180, 180);
    }
}
