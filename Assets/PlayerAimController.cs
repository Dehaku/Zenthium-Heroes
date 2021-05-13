using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAimController : MonoBehaviour
{
    public CharacterController controller;

    public GameObject mainCamera;
    public GameObject aimCamera;
    public GameObject aimReticle;

    // Start is called before the first frame update
    void Start()
    {
        
    }

   

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            
            
            controller.transform.rotation = Quaternion.LookRotation(controller.transform.position, Camera.main.transform.up);
            var lookPosition = controller.transform.position + Camera.main.transform.forward;
            
            controller.transform.LookAt(lookPosition);
          
            mainCamera.SetActive(false);
            aimCamera.SetActive(true);
            aimCamera.transform.position = mainCamera.transform.position;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            mainCamera.SetActive(true);
            aimCamera.SetActive(false);
            mainCamera.transform.position = aimCamera.transform.position;
        }
    }
}
