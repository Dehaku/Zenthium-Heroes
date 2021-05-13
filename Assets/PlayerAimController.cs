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

    public AxisState xAxis;
    public AxisState yAxis;

    public float camMainDistance = 10f;
    public float camShoulderDistance = 4.5f;

    public Vector3 camMainOffset = new Vector3(1.5f, 0, 0);
    public Vector3 camShoulderOffset = new Vector3(1.5f, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
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
            aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camShoulderDistance, Time.deltaTime * 4);
            aim.ShoulderOffset = Vector3.Lerp(aim.ShoulderOffset, camShoulderOffset, Time.deltaTime * 4);

            xAxis.m_MaxSpeed = 100;
            yAxis.m_MaxSpeed = 100;
        }
        else
        {
            aimReticle.SetActive(false);
            aim.CameraDistance = Mathf.Lerp(aim.CameraDistance, camMainDistance, Time.deltaTime * 4);
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
    }
}
