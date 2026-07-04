using UnityEngine;
using UnityEngine.InputSystem;

public partial class GamepadPlayerInput
{
    public void SetGamepadIndex(
        int newIndex)
    {
        StopRumble();
        ResetReelTracking();

        gamepadIndex =
            Mathf.Max(
                0,
                newIndex
            );
    }

    protected override void OnDisable()
    {
        StopRumble();
        ResetReelTracking();

        base.OnDisable();
    }

    private void OnDestroy()
    {
        StopRumble();
    }

    private void OnApplicationPause(
        bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopRumble();
        }
    }

    private void OnApplicationFocus(
        bool hasFocus)
    {
        if (!hasFocus)
        {
            StopRumble();
        }
    }

    private void OnValidate()
    {
        fullReelAngularSpeed =
            Mathf.Max(
                minimumReelAngularSpeed + 1f,
                fullReelAngularSpeed
            );

        rumbleStepDegrees =
            Mathf.Max(
                1f,
                rumbleStepDegrees
            );
    }
}
