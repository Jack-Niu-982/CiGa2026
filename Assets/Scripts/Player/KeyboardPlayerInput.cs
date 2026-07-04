using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 键盘玩家输入。
/// 默认使用WASD移动，E键交互。
/// </summary>
[DefaultExecutionOrder(-100)]
public class KeyboardPlayerInput : PlayerInputBase
{
    [Header("移动按键")]
    [SerializeField] private Key leftKey = Key.A;
    [SerializeField] private Key rightKey = Key.D;
    [SerializeField] private Key upKey = Key.W;
    [SerializeField] private Key downKey = Key.S;

    [Header("交互按键")]
    [Tooltip("玩家靠近设施后，按下该键开始或结束操作。")]
    [SerializeField] private Key interactKey = Key.E;

    [Header("备用按键")]
    [Tooltip("开启后，也可以使用方向键移动。")]
    [SerializeField] private bool allowArrowKeys = true;

    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            ClearInputState();
            return;
        }

        bool leftHeld =
            IsKeyPressed(keyboard, leftKey) ||
            (
                allowArrowKeys &&
                keyboard.leftArrowKey.isPressed
            );

        bool rightHeld =
            IsKeyPressed(keyboard, rightKey) ||
            (
                allowArrowKeys &&
                keyboard.rightArrowKey.isPressed
            );

        bool upHeld =
            IsKeyPressed(keyboard, upKey) ||
            (
                allowArrowKeys &&
                keyboard.upArrowKey.isPressed
            );

        bool downHeld =
            IsKeyPressed(keyboard, downKey) ||
            (
                allowArrowKeys &&
                keyboard.downArrowKey.isPressed
            );

        bool interactHeld =
            IsKeyPressed(
                keyboard,
                interactKey
            );

        float horizontal = 0f;
        float vertical = 0f;

        if (leftHeld)
        {
            horizontal -= 1f;
        }

        if (rightHeld)
        {
            horizontal += 1f;
        }

        if (upHeld)
        {
            vertical += 1f;
        }

        if (downHeld)
        {
            vertical -= 1f;
        }

        SetInputState(
            horizontal,
            vertical,
            upHeld,
            downHeld,
            interactHeld
        );
    }

    private bool IsKeyPressed(
        Keyboard keyboard,
        Key key)
    {
        if (keyboard == null ||
            key == Key.None)
        {
            return false;
        }

        return keyboard[key].isPressed;
    }
}