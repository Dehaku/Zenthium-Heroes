using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindingDisplay : MonoBehaviour
{
    [SerializeField] InputActionReference jumpAction = null;
    [SerializeField] PlayerInput playerInput = null;
    [SerializeField] TMP_Text bindingDisplayNameText = null;
    [SerializeField] GameObject startRebindObject;
    [SerializeField] GameObject waitingForInputObject;

    InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    public void StartRebinding()
    {
        startRebindObject.SetActive(false);
        waitingForInputObject.SetActive(true);

        playerInput.SwitchCurrentActionMap("Menu");

        rebindingOperation = jumpAction.action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => RebindComplete())
            .Start();






        /*
        jumpAction.action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnComplete(callback =>
            {
                Debug.Log(callback.action.bindings[0].overridePath);
                callback.Dispose();
                playerInput.SwitchCurrentActionMap("Player");
            })
            .Start();
        */

    }

    void RebindComplete()
    {
        rebindingOperation.Dispose();

        startRebindObject.SetActive(true);
        waitingForInputObject.SetActive(false);

        playerInput.SwitchCurrentActionMap("Player");
    }
}
