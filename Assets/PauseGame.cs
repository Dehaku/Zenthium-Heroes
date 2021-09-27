using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseGame : MonoBehaviour
{
    //[SerializeField] InputSystem inputSystem;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] InputAction pauseButton;
    [SerializeField] Canvas canvas;
    InputSettings.UpdateMode storedUpdateMode;

    bool paused = false;
    float previousTimescale = 1f;

    private void OnEnable()
    {
        pauseButton.Enable();
    }
    private void OnDisable()
    {
        pauseButton.Disable();
    }

    private void Start()
    {
        //playerInput.actions["PauseGame"].performed += _ => Pause();
        pauseButton.performed += _ => Pause();
    }

    public void Pause()
    {
        paused = !paused;
        if(paused)
        {
            // Store current settings for later
            storedUpdateMode = InputSystem.settings.updateMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;

            previousTimescale = Time.timeScale;
            Time.timeScale = 0f;
            
            //Rebinding stuff
            canvas.enabled = true;
            playerInput.currentActionMap.Disable();



        }
        else
        {
            // Restore settings from earlier
            InputSystem.settings.updateMode = storedUpdateMode;
            Time.timeScale = previousTimescale;

            //Rebinding stuff
            canvas.enabled = false;
            playerInput.currentActionMap.Enable();
        }
    }
}
