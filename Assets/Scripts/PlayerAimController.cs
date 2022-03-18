using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using DG.Tweening;

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

    bool isAiming = false;

    Cinemachine3rdPersonFollow aim;
    CinemachineVirtualCamera aimSettings;

    public LayerMask aimLayer;

    // Start is called before the first frame update
    void Start()
    {
        aimSettings = aimCamera.GetComponent<CinemachineVirtualCamera>();
        aim = aimSettings.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        // Starting with the main cam.
        MainCam();
    }

    void MainCam()
    {
        aimReticle.SetActive(false);
        aimReticle2.SetActive(false);
        //aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camMainDistance, Time.deltaTime * 4);
        //aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camMainOffset, Time.deltaTime * 4);
        //aimSettings.m_Lens.FieldOfView = Mathf.Lerp(aimSettings.m_Lens.FieldOfView, camMainFOV, Time.deltaTime * 4);

        DOTween.To(() => aim.CameraDistance, x => aim.CameraDistance = x, camMainDistance, 0.25f);
        DOTween.To(() => aim.ShoulderOffset, x => aim.ShoulderOffset = x, camMainOffset, 0.25f);
        DOTween.To(() => aimSettings.m_Lens.FieldOfView, x => aimSettings.m_Lens.FieldOfView = x, camMainFOV, 0.25f);

        xAxis.m_MaxSpeed = 500;
        yAxis.m_MaxSpeed = 300;

        

        

        sensitivity = defaultSensitivity;
        stickSensitivity = defaultStickSensitivity;
        isAiming = false;
    }

    void ShoulderCam()
    {
        aimReticle.SetActive(true);
        aimReticle2.SetActive(true);
        // aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camShoulderDistance, Time.deltaTime * 4);
        // aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camShoulderOffset, Time.deltaTime * 4);
        // aimSettings.m_Lens.FieldOfView = Mathf.Lerp(aimSettings.m_Lens.FieldOfView, camShoulderFOV, Time.deltaTime * 4);

        DOTween.To(() => aim.CameraDistance, x => aim.CameraDistance = x, camShoulderDistance, 0.25f);
        DOTween.To(() => aim.ShoulderOffset, x => aim.ShoulderOffset = x, camShoulderOffset, 0.25f);
        DOTween.To(() => aimSettings.m_Lens.FieldOfView, x => aimSettings.m_Lens.FieldOfView = x, camShoulderFOV, 0.25f);

        //= Mathf.Lerp(aim.CameraDistance, camShoulderDistance, Time.deltaTime * 4);
        //aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camShoulderOffset, Time.deltaTime * 4);
        //aimSettings.m_Lens.FieldOfView = Mathf.Lerp(aimSettings.m_Lens.FieldOfView, camShoulderFOV, Time.deltaTime * 4);


        xAxis.m_MaxSpeed = 100;
        yAxis.m_MaxSpeed = 100;

        

        

        sensitivity = defaultZoomedSensitivity;
        stickSensitivity = defaultStickZoomedSensitivity;
        isAiming = true;
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

    public void Aim(InputAction.CallbackContext context)
    {
        if(context.action.WasPressedThisFrame())
        {
            ShoulderCam();
            thirdPersonController.SetRotateOnMove(false);
        }
        else if (context.action.WasReleasedThisFrame())
        {
            MainCam();
            thirdPersonController.SetRotateOnMove(true);
        }
    }

    void CycleCameraDistance()
    {
        var mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll < 0)
            camShoulderFOV += 3;
        if (mouseScroll > 0)
            camShoulderFOV -= 3;
        camShoulderFOV = Mathf.Clamp(camShoulderFOV, 10, camMainFOV);
    }

    void CycleCameraZoom()
    {
        var mouseScroll = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScroll < 0)
            camMainDistance += 3;
        if (mouseScroll > 0)
            camMainDistance -= 3;

        camMainDistance = Mathf.Clamp(camMainDistance, camShoulderDistance, camMainMaxZoom);
    }


    void CycleCameraDistanceAndZoom()
    {
        
        if(isAiming)
        {
            // CycleCameraDistance();
        }
        else
        {
            // CycleCameraZoom();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Always had problems if the camera follow target was a child of the player, this is a work around.
        Vector3 aimOffset = transform.position;
        aimOffset.y += aimLookAtOffset.y;
        aimOffset += transform.forward * aimLookAtOffset.z; // This is to put it slightly behind the player, so rubbing against walls doesn't freak it out as easily.
        aimLookAt.transform.position = aimOffset;
        //aimLookAt.transform.position = transform.position + aimLookAtOffset;

        if (Cursor.lockState != CursorLockMode.Locked)
        { return;  }

        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);

        aimInput.x += Input.GetAxisRaw("Mouse X") * sensitivity;
        aimInput.y += -Input.GetAxisRaw("Mouse Y") * sensitivity;

        //aimLookAt.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, 0);
        //aimLookAt.eulerAngles = new Vector3(aimInput.x, aimInput.y, 0);
        aimLookAt.eulerAngles = new Vector3(aimInput.y, aimInput.x, 0);

        AimReticleAdjust();

        

        
        
        if (isAiming)
        {
            

            Ray camRay = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);

            if (!Physics.Raycast(camRay, out RaycastHit camHit, Mathf.Infinity, aimLayer, QueryTriggerInteraction.Ignore)) { }

            Vector3 worldAimTarget = camHit.point;
            worldAimTarget.y = controller.transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = aimDirection;
        }

        CycleCameraDistanceAndZoom();
        //if (Input.GetMouseButton(1))
        //    ShoulderCam();
        //else
        //    MainCam();
        
    }

    private void AimReticleAdjust()
    {
        


        Vector3 center = aimLookAt.transform.position;
        Ray camRay = Camera.main.ViewportPointToRay(Vector3.one * 0.5f);
        if (!Physics.Raycast(camRay, out RaycastHit camHit, Mathf.Infinity, aimLayer, QueryTriggerInteraction.Ignore)) { }

        Ray ray = new Ray(center, (camHit.point - center).normalized * 100);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, aimLayer,QueryTriggerInteraction.Ignore)) { }

        aimReticle.transform.position = Camera.main.WorldToScreenPoint(hit.point);
    }

    public void AimReticleAdjust(Vector3 pos)
    {
        aimReticle.transform.position = Camera.main.WorldToScreenPoint(pos);
    }

    public void Look(InputAction.CallbackContext context)
    {
        //var inputValue = context.ReadValue<Vector2>();
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
