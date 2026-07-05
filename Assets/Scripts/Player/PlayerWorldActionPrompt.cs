using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerWorldActionPrompt : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerOperateInteractor2D operateInteractor;

    [SerializeField]
    private PlayerCarryInteractor2D carryInteractor;

    [SerializeField]
    private PlayerPutInInteractor2D putInInteractor;

    [SerializeField]
    private KeyboardPlayerInput keyboardInput;

    [SerializeField]
    private GamepadPlayerInput gamepadInput;

    [SerializeField]
    private TMP_Text promptText;

    private readonly List<string> actionLines =
        new List<string>(2);

    private void Awake()
    {
        ResolveReferences();
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController =
                GetComponentInParent<PlayerController>();
        }

        if (operateInteractor == null)
        {
            operateInteractor =
                GetComponentInParent
                    <PlayerOperateInteractor2D>();
        }

        if (carryInteractor == null)
        {
            carryInteractor =
                GetComponentInParent
                    <PlayerCarryInteractor2D>();
        }

        if (putInInteractor == null)
        {
            putInInteractor =
                GetComponentInParent
                    <PlayerPutInInteractor2D>();
        }

        if (keyboardInput == null)
        {
            keyboardInput =
                GetComponentInParent
                    <KeyboardPlayerInput>();
        }

        if (gamepadInput == null)
        {
            gamepadInput =
                GetComponentInParent
                    <GamepadPlayerInput>();
        }

        if (promptText == null)
        {
            promptText =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }
    }

    private void Refresh()
    {
        if (promptText == null)
        {
            return;
        }

        actionLines.Clear();

        if (operateInteractor != null &&
            operateInteractor.HasInteractOption)
        {
            actionLines.Add(
                BuildActionLine(
                    GetInteractKeyLabel(),
                    GetInteractButtonLabel(),
                    "Interact"
                )
            );
        }

        if (carryInteractor != null &&
            carryInteractor.HasPickUpOption)
        {
            actionLines.Add(
                BuildActionLine(
                    GetPickUpKeyLabel(),
                    GetPickUpButtonLabel(),
                    "PickUp"
                )
            );
        }

        if (putInInteractor != null &&
            putInInteractor.HasPutInOption)
        {
            actionLines.Add(
                BuildActionLine(
                    GetPutInKeyLabel(),
                    GetPutInButtonLabel(),
                    "PutIn"
                )
            );
        }

        promptText.text =
            string.Join("\n", actionLines);

        promptText.enabled =
            actionLines.Count > 0;
    }

    private string GetInteractKeyLabel()
    {
        return keyboardInput != null
            ? keyboardInput.InteractKey.ToString()
            : "F";
    }

    private string GetPickUpKeyLabel()
    {
        return keyboardInput != null
            ? keyboardInput.PickUpKey.ToString()
            : "E";
    }

    private string GetPutInKeyLabel()
    {
        return keyboardInput != null
            ? keyboardInput.PutInKey.ToString()
            : "G";
    }

    private string GetInteractButtonLabel()
    {
        return gamepadInput != null
            ? gamepadInput.InteractButtonBinding.ToString()
            : "South";
    }

    private string GetPickUpButtonLabel()
    {
        return gamepadInput != null
            ? gamepadInput.PickUpButtonBinding.ToString()
            : "West";
    }

    private string GetPutInButtonLabel()
    {
        return gamepadInput != null
            ? gamepadInput.PutInButtonBinding.ToString()
            : "West";
    }

    private string BuildActionLine(
        string keyLabel,
        string buttonLabel,
        string actionLabel)
    {
        string bindingLabel =
            IsUsingGamepad()
                ? buttonLabel
                : keyLabel;

        return $"[{bindingLabel}]{actionLabel}";
    }

    private bool IsUsingGamepad()
    {
        if (playerController != null &&
            playerController.CurrentInput != null)
        {
            return
                playerController.CurrentInput
                    is GamepadPlayerInput;
        }

        return
            gamepadInput != null &&
            gamepadInput.enabled &&
            (keyboardInput == null ||
             !keyboardInput.enabled);
    }
}
