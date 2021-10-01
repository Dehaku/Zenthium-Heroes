using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestingInputSystem : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private PlayerInput playerInputs;

    Vector3 inputMovement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //playerInputs = GetComponent<PlayerInput>();
        
        //playerInputs.actions.

    }

    public void Movement(InputAction.CallbackContext context)
    {
        //Debug.Log(context);
        var inputValue = context.ReadValue<Vector2>();
        inputMovement = inputValue;

        if (context.performed)
        {
            //Debug.Log("Moving" + context.phase);
            
        }

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            //string map = "Driving";
            //Debug.Log("Switching to" + map);
            //playerInputs.SwitchCurrentActionMap(map);
        }
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            //string map = "Player";
            //Debug.Log("Switching to" + map);
            //playerInputs.SwitchCurrentActionMap(map);
        }
        if (Keyboard.current.cKey.isPressed)
        {
            Debug.Log("C!");
        }

        



        if (playerInputs.actions["PowerModify"].IsPressed())
        {
            //IsPressed() runs every frame.
            if(playerInputs.actions["Jump"].WasPressedThisFrame())
            {
                // Runs once
                //Debug.Log("Jump Modified!");
            }
        }

        if(playerInputs.actions["SuperJump"].WasPressedThisFrame())
        {
            //Debug.Log("!!Super Jump!!");
        }


        float speed = 5f;
        rb.AddForce(new Vector3(inputMovement.x, 0, inputMovement.y) * speed, ForceMode.Force);
    }

    public void Jump(InputAction.CallbackContext context)
    {
        //Debug.Log(context);
        if(context.performed)
        {
            //Debug.Log("Jump." + context.phase);
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }
        
    }

    public void Handbreak(InputAction.CallbackContext context)
    {
        //Debug.Log(context);
        if (context.performed)
        {
            //Debug.Log("Handbreak." + context.phase);
            //rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    
}
