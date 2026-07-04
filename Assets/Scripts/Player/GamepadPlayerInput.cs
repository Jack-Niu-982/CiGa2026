using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 手柄玩家输入。
/// 通过Gamepad Index指定读取第几个手柄。
/// </summary>
[DefaultExecutionOrder(-100)]
public class GamepadPlayerInput : PlayerInputBase
{
    public enum InteractButton
    {
        South,
        East,
        West,
        North,
        LeftShoulder,
        RightShoulder
    }

    [Header("手柄分配")]
    [Tooltip("0代表第一个手柄，1代表第二个手柄，以此类推。")]
    [Min(0)]
    [SerializeField] private int gamepadIndex;

    [Header("摇杆设置")]
    [Range(0f, 0.95f)]
    [SerializeField] private float stickDeadZone = 0.2f;

    [Tooltip("摇杆上下超过这个数值后，才认为按下了上或下。")]
    [Range(0.1f, 1f)]
    [SerializeField] private float verticalInputThreshold = 0.6f;

    [Header("交互按键")]
    [Tooltip(
        "默认West：Xbox为X，PlayStation为方块，Switch通常为Y。"
    )]
    [SerializeField]
    private InteractButton interactButton =
        InteractButton.West;

    [Header("附加操作")]
    [Tooltip(
        "开启后，手柄南键也可以执行向上操作和跳跃。"
    )]
    [SerializeField]
    private bool southButtonAlsoActsAsUp;

    public int GamepadIndex =>
        gamepadIndex;

    private void Update()
    {
        Gamepad gamepad =
            GetAssignedGamepad();

        if (gamepad == null)
        {
            ClearInputState();
            return;
        }

        Vector2 stickInput =
            gamepad.leftStick.ReadValue();

        Vector2 dpadInput =
            gamepad.dpad.ReadValue();

        // 十字键有输入时，优先使用十字键。
        Vector2 moveInput =
            dpadInput.sqrMagnitude > 0.01f
                ? dpadInput
                : stickInput;

        if (Mathf.Abs(moveInput.x) <
            stickDeadZone)
        {
            moveInput.x = 0f;
        }

        if (Mathf.Abs(moveInput.y) <
            stickDeadZone)
        {
            moveInput.y = 0f;
        }

        bool upHeld =
            moveInput.y >=
            verticalInputThreshold;

        bool downHeld =
            moveInput.y <=
            -verticalInputThreshold;

        if (southButtonAlsoActsAsUp)
        {
            upHeld |=
                gamepad.buttonSouth.isPressed;
        }

        bool interactHeld =
            IsInteractButtonPressed(
                gamepad
            );

        SetInputState(
            moveInput.x,
            moveInput.y,
            upHeld,
            downHeld,
            interactHeld
        );
    }

    private bool IsInteractButtonPressed(
        Gamepad gamepad)
    {
        switch (interactButton)
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

    /// <summary>
    /// 供之后的多人输入管理器指定手柄编号。
    /// </summary>
    public void SetGamepadIndex(
        int newIndex)
    {
        gamepadIndex =
            Mathf.Max(
                0,
                newIndex
            );
    }
}