using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerAimController : MonoBehaviour
{
    public CharacterController controller;

    public GameObject aimCamera;
    public Transform aimLookAt;
    public GameObject aimReticle;
    public GameObject aimReticle2;
    public float sensitivity = 1;

    public AxisState xAxis;
    public AxisState yAxis;

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
    }

    

    // Update is called once per frame
    void Update()
    {
        
        if(Cursor.lockState != CursorLockMode.Locked)
        { return;  }
        
        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);

        aimLookAt.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, 0);

        AimReticleAdjust();




        if (Input.GetMouseButtonDown(1))
        {
            //controller.transform.rotation = Quaternion.LookRotation(controller.transform.position, Camera.main.transform.up);
            var lookPosition = controller.transform.position + Camera.main.transform.forward;

            //controller.transform.LookAt(lookPosition);

            
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
}
