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

    private const string RebindsKey = "rebinds";

    private void Start()
    {
        string rebinds = PlayerPrefs.GetString(RebindsKey, string.Empty);
        if(string.IsNullOrEmpty(rebinds)) { return; }

        playerInput.actions.LoadBindingOverridesFromJson(rebinds);

        int bindingIndex = jumpAction.action.GetBindingIndexForControl(jumpAction.action.controls[0]);

        bindingDisplayNameText.text = InputControlPath.ToHumanReadableString(
            jumpAction.action.bindings[0].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void Save()
    {
        string rebinds = playerInput.actions.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(RebindsKey, rebinds);
    }


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
    }

    void RebindComplete()
    {
        int bindingIndex = jumpAction.action.GetBindingIndexForControl(jumpAction.action.controls[0]);

        bindingDisplayNameText.text = InputControlPath.ToHumanReadableString(
            jumpAction.action.bindings[0].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        rebindingOperation.Dispose();

        startRebindObject.SetActive(true);
        waitingForInputObject.SetActive(false);

        playerInput.SwitchCurrentActionMap("Player");
    }
}
