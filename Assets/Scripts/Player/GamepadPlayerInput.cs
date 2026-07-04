using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 手柄玩家输入。
/// 通过 Gamepad Index 指定读取第几个手柄。
///
/// 船锚：
/// R1 发射。
/// South 松开并回收。
/// 顺时针持续转动右摇杆主动收紧绳子。
///
/// South 可以同时作为普通交互键和船锚回收键，
/// 具体是否触发由玩家当前状态决定。
/// </summary>
[DefaultExecutionOrder(-100)]
public partial class GamepadPlayerInput : PlayerInputBase
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

    public enum ReelRotationDirection
    {
        Clockwise,
        CounterClockwise,
        EitherDirection
    }

    [Header("手柄分配")]

    [Tooltip(
        "0 代表第一个手柄，" +
        "1 代表第二个手柄，以此类推。"
    )]
    [Min(0)]
    [SerializeField]
    private int gamepadIndex;

    [Header("左摇杆移动")]

    [Range(0f, 0.95f)]
    [SerializeField]
    private float stickDeadZone = 0.2f;

    [Tooltip(
        "左摇杆上下超过这个数值后，" +
        "才认为按下了上或下。"
    )]
    [Range(0.1f, 1f)]
    [SerializeField]
    private float verticalInputThreshold = 0.6f;

    [Header("交互按键")]

    [Tooltip(
        "默认 South。进入设备操作状态后，" +
        "应由玩家状态关闭普通拾取。"
    )]
    [SerializeField]
    private InteractButton interactButton =
        InteractButton.South;

    [Header("船锚按键")]

    [Tooltip("R1 / Right Shoulder：发射船锚。")]
    [SerializeField]
    private bool useRightShoulderToShoot = true;

    [Tooltip("South：松开锚点并开始回收船锚。")]
    [SerializeField]
    private bool useSouthToRetract = true;

    [Header("右摇杆绞盘")]

    [Tooltip(
        "右摇杆需要按照哪个方向持续旋转，" +
        "才能收紧绳子。"
    )]
    [SerializeField]
    private ReelRotationDirection reelRotationDirection =
        ReelRotationDirection.Clockwise;

    [Tooltip(
        "右摇杆长度超过该值后才检测绕圈，" +
        "避免摇杆回中时产生角度抖动。"
    )]
    [Range(0.1f, 0.99f)]
    [SerializeField]
    private float reelStickMinimumMagnitude = 0.65f;

    [Tooltip(
        "低于该角速度时不算有效收绳。" +
        "单位为度/秒。"
    )]
    [Min(1f)]
    [SerializeField]
    private float minimumReelAngularSpeed = 45f;

    [Tooltip(
        "达到该角速度时，" +
        "收绳输入强度视为 1。"
    )]
    [Min(1f)]
    [SerializeField]
    private float fullReelAngularSpeed = 360f;

    [Tooltip(
        "识别到有效转动时，" +
        "至少输出多少收绳强度。"
    )]
    [Range(0.01f, 1f)]
    [SerializeField]
    private float minimumReelAmount = 0.25f;

    [Tooltip(
        "停止有效转动后，保留输入的极短时间，" +
        "减少不同帧之间的断续感。"
    )]
    [Range(0f, 0.3f)]
    [SerializeField]
    private float reelInputGraceTime = 0.08f;

    [Tooltip(
        "单帧角度变化超过该值时视为摇杆跳变，" +
        "不计入收绳。"
    )]
    [Range(30f, 180f)]
    [SerializeField]
    private float maximumAcceptedAngleDelta = 120f;

    [Tooltip("收绳输入上升速度。")]
    [Min(0.1f)]
    [SerializeField]
    private float reelAmountRiseSpeed = 10f;

    [Tooltip("收绳输入下降速度。")]
    [Min(0.1f)]
    [SerializeField]
    private float reelAmountFallSpeed = 14f;

    [Header("收绳震动")]

    [Tooltip("是否启用船锚手柄震动。")]
    [SerializeField]
    private bool enableAnchorRumble = true;

    [Tooltip("每转过多少度产生一次棘轮震动。")]
    [Range(10f, 180f)]
    [SerializeField]
    private float rumbleStepDegrees = 45f;

    [Tooltip("每次棘轮震动持续时间。")]
    [Range(0.01f, 0.2f)]
    [SerializeField]
    private float reelTickDuration = 0.045f;

    [Tooltip("棘轮震动的低频强度。")]
    [Range(0f, 1f)]
    [SerializeField]
    private float reelTickLowFrequency = 0.42f;

    [Tooltip("棘轮震动的高频强度。")]
    [Range(0f, 1f)]
    [SerializeField]
    private float reelTickHighFrequency = 0.22f;

    [Tooltip("持续收绳达到最大速度时的低频震动。")]
    [Range(0f, 1f)]
    [SerializeField]
    private float continuousReelLowFrequency = 0.18f;

    [Tooltip("持续收绳达到最大速度时的高频震动。")]
    [Range(0f, 1f)]
    [SerializeField]
    private float continuousReelHighFrequency = 0.07f;

    [Header("发射震动")]

    [Range(0.01f, 0.5f)]
    [SerializeField]
    private float shootRumbleDuration = 0.12f;

    [Range(0f, 1f)]
    [SerializeField]
    private float shootLowFrequency = 0.25f;

    [Range(0f, 1f)]
    [SerializeField]
    private float shootHighFrequency = 0.65f;

    [Header("回收震动")]

    [Range(0.01f, 0.5f)]
    [SerializeField]
    private float retractRumbleDuration = 0.16f;

    [Range(0f, 1f)]
    [SerializeField]
    private float retractLowFrequency = 0.55f;

    [Range(0f, 1f)]
    [SerializeField]
    private float retractHighFrequency = 0.18f;

    [Header("附加操作")]

    [Tooltip(
        "开启后，手柄 South 也可以执行向上或跳跃。" +
        "一般建议关闭，避免和交互、回收重复。"
    )]
    [SerializeField]
    private bool southButtonAlsoActsAsUp;

    private bool anchorControlActive;
    private bool controlledAnchorIsAttached;

    private bool hasPreviousReelAngle;
    private float previousReelAngle;

    private float currentReelAmount;
    private float reelGraceTimer;
    private float accumulatedReelDegrees;

    private float reelTickTimer;

    private float actionRumbleTimer;
    private float actionRumbleLowFrequency;
    private float actionRumbleHighFrequency;

    private Gamepad rumbleGamepad;

    private float lastLowFrequency = -1f;
    private float lastHighFrequency = -1f;

    public int GamepadIndex =>
        gamepadIndex;

    private void Update()
    {
        Gamepad gamepad =
            GetAssignedGamepad();

        if (gamepad == null)
        {
            StopRumble();
            ResetReelTracking();
            ClearInputState();
            return;
        }

        Vector2 stickInput =
            gamepad.leftStick.ReadValue();

        Vector2 dpadInput =
            gamepad.dpad.ReadValue();

        // 十字键有输入时优先使用十字键。
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
            IsInteractButtonPressed(gamepad);

        bool anchorShootHeld =
            useRightShoulderToShoot &&
            gamepad.rightShoulder.isPressed;

        bool anchorRetractHeld =
            useSouthToRetract &&
            gamepad.buttonSouth.isPressed;

        UpdateRightStickReelInput(gamepad);

        SetInputState(
            moveInput.x,
            moveInput.y,
            upHeld,
            downHeld,
            interactHeld,
            anchorShootHeld,
            anchorRetractHeld,
            currentReelAmount
        );

        UpdateAnchorRumble(gamepad);
    }
}
