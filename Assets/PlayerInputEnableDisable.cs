using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputEnableDisable : MonoBehaviour
{
    public PlayerInput playerInput;

    // This script is an attempt to fix the random inputs being launched when the game starts.
    private void OnEnable()
    {
        playerInput.currentActionMap.Enable();
    }

    private void OnDisable()
    {
        if(playerInput.currentActionMap != null)
            playerInput.currentActionMap.Disable();
    }
}
