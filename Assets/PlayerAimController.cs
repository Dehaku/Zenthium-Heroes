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
    public float sensitivity = 1;

    public Cinemachine.AxisState xAxis;
    public Cinemachine.AxisState yAxis;

    public float camMainDistance = 10f;
    public float camShoulderDistance = 4.5f;
    float _camTransitDistance;

    public Vector3 camMainOffset = new Vector3(1.5f, 0, 0);
    public Vector3 camShoulderOffset = new Vector3(1.5f, 0, 0);
    Vector3 _camTransitOffset;



    // Start is called before the first frame update
    void Start()
    {
        _camTransitDistance = camMainDistance;
        _camTransitOffset = camMainOffset;
    }

   

    // Update is called once per frame
    void Update()
    {

        




        
        

        xAxis.Update(Time.deltaTime);
        yAxis.Update(Time.deltaTime);

        aimLookAt.eulerAngles = new Vector3(yAxis.Value, xAxis.Value, 0);

        var aim = aimCamera.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<Cinemachine3rdPersonFollow>();

        if (Input.GetMouseButtonDown(1))
        {
            controller.transform.rotation = Quaternion.LookRotation(controller.transform.position, Camera.main.transform.up);
            var lookPosition = controller.transform.position + Camera.main.transform.forward;

            controller.transform.LookAt(lookPosition);
        }

        if (Input.GetMouseButton(1))
        {
            aimReticle.SetActive(true);
            //aim.CameraDistance = camShoulderDistance;
            aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camShoulderDistance, Time.deltaTime * 4);
            //aim.ShoulderOffset = camShoulderOffset;
            aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camShoulderOffset, Time.deltaTime * 4);

            xAxis.m_MaxSpeed = 100;
            yAxis.m_MaxSpeed = 100;
        }
        else
        {
            aimReticle.SetActive(false);
            //aim.CameraDistance = camMainDistance;
            aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camMainDistance, Time.deltaTime * 4);
            //aim.ShoulderOffset = camMainOffset;
            aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camMainOffset, Time.deltaTime * 4);
            xAxis.m_MaxSpeed = 500;
            yAxis.m_MaxSpeed = 300;

            var mouseScroll = Input.GetAxis("Mouse ScrollWheel");
            if (mouseScroll < 0)
                camMainDistance += 3;
            if (mouseScroll > 0)
                camMainDistance -= 3;

            camMainDistance = Mathf.Clamp(camMainDistance,camShoulderDistance,20);
            

        }
            

        if (aimCamera.activeSelf)
        {
            float vertical = Input.GetAxis("Mouse Y") * sensitivity;
            float horizontal = Input.GetAxis("Mouse X") * sensitivity;

            
            

            // aimLookAt.transform.Rotate(new Vector3(vertical,horizontal, -aimLookAt.transform.rotation.z));

            

            //aimLookAt.transform.position = new Vector3(aimLookAt.transform.position.x, aimLookAt.transform.position.y+vertical, aimLookAt.transform.position.z);


        }
    }
}
