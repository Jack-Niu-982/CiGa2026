using UnityEngine.InputSystem;

public partial class GamepadPlayerInput
{
    private bool IsButtonPressed(
        Gamepad gamepad,
        InteractButton button)
    {
        switch (button)
        {
            case InteractButton.South:
                return gamepad
                    .buttonSouth
                    .isPressed;

            case InteractButton.East:
                return gamepad
                    .buttonEast
                    .isPressed;

            case InteractButton.North:
                return gamepad
                    .buttonNorth
                    .isPressed;

            case InteractButton.LeftShoulder:
                return gamepad
                    .leftShoulder
                    .isPressed;

            case InteractButton.RightShoulder:
                return gamepad
                    .rightShoulder
                    .isPressed;

            case InteractButton.West:
            default:
                return gamepad
                    .buttonWest
                    .isPressed;
        }
    }

    private Gamepad GetAssignedGamepad()
    {
        if (gamepadIndex < 0 ||
            gamepadIndex >= Gamepad.all.Count)
        {
            return null;
        }

        return Gamepad.all[gamepadIndex];
    }
}
