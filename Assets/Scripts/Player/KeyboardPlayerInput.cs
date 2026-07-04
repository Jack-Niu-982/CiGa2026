using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 键盘玩家输入。
///
/// 默认：
/// WASD 移动。
/// F 交互。
///
/// 船锚：
/// Q 发射。
/// R 回收。
/// E 按住拉回。
/// </summary>
[DefaultExecutionOrder(-100)]
public class KeyboardPlayerInput : PlayerInputBase
{
    [Header("移动按键")]

    [SerializeField]
    private Key leftKey = Key.A;

    [SerializeField]
    private Key rightKey = Key.D;

    [SerializeField]
    private Key upKey = Key.W;

    [SerializeField]
    private Key downKey = Key.S;

    [Header("交互按键")]

    [Tooltip("E 已用于船锚拉回，因此默认把交互改为 F。")]
    [SerializeField]
    private Key interactKey = Key.F;

    [Header("船锚按键")]

    [Tooltip("按下后发射船锚。")]
    [SerializeField]
    private Key anchorShootKey = Key.Q;

    [Tooltip("按下后松开锚点并开始回收船锚。")]
    [SerializeField]
    private Key anchorRetractKey = Key.R;

    [Tooltip("按住后主动拉回绳子。")]
    [SerializeField]
    private Key anchorReelKey = Key.E;

    [Header("备用按键")]

    [Tooltip("开启后，也可以使用方向键移动。")]
    [SerializeField]
    private bool allowArrowKeys = true;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

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

        bool anchorShootHeld =
            IsKeyPressed(
                keyboard,
                anchorShootKey
            );

        bool anchorRetractHeld =
            IsKeyPressed(
                keyboard,
                anchorRetractKey
            );

        bool anchorReelHeld =
            IsKeyPressed(
                keyboard,
                anchorReelKey
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
            interactHeld,
            anchorShootHeld,
            anchorRetractHeld,
            anchorReelHeld
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