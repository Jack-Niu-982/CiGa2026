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

    private void UpdateRightStickReelInput(
        Gamepad gamepad)
    {
        float deltaTime =
            Mathf.Max(
                Time.unscaledDeltaTime,
                0.0001f
            );

        /*
         * 只有玩家正在操控船锚，
         * 并且船锚已经固定时，
         * 才允许右摇杆绕圈收绳。
         */
        if (!anchorControlActive ||
            !controlledAnchorIsAttached)
        {
            hasPreviousReelAngle = false;
            reelGraceTimer = 0f;
            accumulatedReelDegrees = 0f;

            currentReelAmount =
                Mathf.MoveTowards(
                    currentReelAmount,
                    0f,
                    reelAmountFallSpeed *
                    deltaTime
                );

            return;
        }

        Vector2 rightStick =
            gamepad.rightStick.ReadValue();

        float targetReelAmount = 0f;
        bool validRotationThisFrame = false;

        if (rightStick.magnitude >=
            reelStickMinimumMagnitude)
        {
            float currentAngle =
                Mathf.Atan2(
                    rightStick.y,
                    rightStick.x
                ) * Mathf.Rad2Deg;

            if (!hasPreviousReelAngle)
            {
                hasPreviousReelAngle = true;
                previousReelAngle = currentAngle;
            }
            else
            {
                float rawAngleDelta =
                    Mathf.DeltaAngle(
                        previousReelAngle,
                        currentAngle
                    );

                previousReelAngle =
                    currentAngle;

                /*
                 * 如果单帧角度变化太大，
                 * 通常代表摇杆刚刚穿过中心或者发生跳变。
                 */
                if (Mathf.Abs(rawAngleDelta) <=
                    maximumAcceptedAngleDelta)
                {
                    float reelAngleDelta =
                        GetValidReelAngleDelta(
                            rawAngleDelta
                        );

                    if (reelAngleDelta > 0f)
                    {
                        float angularSpeed =
                            reelAngleDelta /
                            deltaTime;

                        if (angularSpeed >=
                            minimumReelAngularSpeed)
                        {
                            targetReelAmount =
                                Mathf.InverseLerp(
                                    minimumReelAngularSpeed,
                                    Mathf.Max(
                                        minimumReelAngularSpeed + 1f,
                                        fullReelAngularSpeed
                                    ),
                                    angularSpeed
                                );

                            targetReelAmount =
                                Mathf.Max(
                                    minimumReelAmount,
                                    targetReelAmount
                                );

                            validRotationThisFrame = true;

                            reelGraceTimer =
                                reelInputGraceTime;

                            accumulatedReelDegrees +=
                                reelAngleDelta;

                            TriggerReelTicksIfNeeded();
                        }
                    }
                }
            }
        }
        else
        {
            /*
             * 回到摇杆中心后重新开始记录角度，
             * 避免下一次推出摇杆时角度跳变。
             */
            hasPreviousReelAngle = false;
        }

        /*
         * 给有效输入一点点缓冲时间，
         * 避免玩家转圆时每个方向交界处断一下。
         */
        if (!validRotationThisFrame &&
            reelGraceTimer > 0f)
        {
            reelGraceTimer -=
                deltaTime;

            targetReelAmount =
                Mathf.Max(
                    minimumReelAmount,
                    currentReelAmount
                );
        }

        float changeSpeed =
            targetReelAmount >
            currentReelAmount
                ? reelAmountRiseSpeed
                : reelAmountFallSpeed;

        currentReelAmount =
            Mathf.MoveTowards(
                currentReelAmount,
                targetReelAmount,
                changeSpeed *
                deltaTime
            );
    }

    private float GetValidReelAngleDelta(
        float rawAngleDelta)
    {
        switch (reelRotationDirection)
        {
            case ReelRotationDirection.CounterClockwise:
                return Mathf.Max(
                    0f,
                    rawAngleDelta
                );

            case ReelRotationDirection.EitherDirection:
                return Mathf.Abs(
                    rawAngleDelta
                );

            case ReelRotationDirection.Clockwise:
            default:
                /*
                 * Unity 中角度正方向是逆时针，
                 * 所以顺时针变化是负数。
                 */
                return Mathf.Max(
                    0f,
                    -rawAngleDelta
                );
        }
    }

    private void TriggerReelTicksIfNeeded()
    {
        float stepDegrees =
            Mathf.Max(
                1f,
                rumbleStepDegrees
            );

        while (accumulatedReelDegrees >=
            stepDegrees)
        {
            accumulatedReelDegrees -=
                stepDegrees;

            reelTickTimer =
                Mathf.Max(
                    reelTickTimer,
                    reelTickDuration
                );
        }
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
    /// AnchorLauncher2D 会在玩家实际操控设备时调用。
    /// </summary>
    public override void SetAnchorControlFeedback(
        bool isControllingAnchor,
        bool anchorIsAttached)
    {
        bool controlChanged =
            anchorControlActive !=
            isControllingAnchor;

        bool attachedChanged =
            controlledAnchorIsAttached !=
            anchorIsAttached;

        anchorControlActive =
            isControllingAnchor;

        controlledAnchorIsAttached =
            anchorIsAttached;

        if (controlChanged ||
            attachedChanged)
        {
            ResetReelTracking();
        }

        if (!anchorControlActive)
        {
            StopRumble();
        }
    }

    public override void PlayAnchorShootFeedback()
    {
        if (!anchorControlActive ||
            !enableAnchorRumble)
        {
            return;
        }

        StartActionRumble(
            shootRumbleDuration,
            shootLowFrequency,
            shootHighFrequency
        );
    }

    public override void PlayAnchorRetractFeedback()
    {
        if (!anchorControlActive ||
            !enableAnchorRumble)
        {
            return;
        }

        StartActionRumble(
            retractRumbleDuration,
            retractLowFrequency,
            retractHighFrequency
        );
    }

    private void StartActionRumble(
        float duration,
        float lowFrequency,
        float highFrequency)
    {
        actionRumbleTimer =
            Mathf.Max(
                actionRumbleTimer,
                duration
            );

        actionRumbleLowFrequency =
            Mathf.Clamp01(
                lowFrequency
            );

        actionRumbleHighFrequency =
            Mathf.Clamp01(
                highFrequency
            );
    }

    private void UpdateAnchorRumble(
        Gamepad gamepad)
    {
        if (!enableAnchorRumble ||
            !anchorControlActive)
        {
            ApplyRumble(
                gamepad,
                0f,
                0f
            );

            return;
        }

        float deltaTime =
            Time.unscaledDeltaTime;

        reelTickTimer =
            Mathf.Max(
                0f,
                reelTickTimer -
                deltaTime
            );

        actionRumbleTimer =
            Mathf.Max(
                0f,
                actionRumbleTimer -
                deltaTime
            );

        float lowFrequency = 0f;
        float highFrequency = 0f;

        /*
         * 转得越快，持续震动越强。
         */
        if (controlledAnchorIsAttached &&
            currentReelAmount > 0.01f)
        {
            lowFrequency =
                continuousReelLowFrequency *
                currentReelAmount;

            highFrequency =
                continuousReelHighFrequency *
                currentReelAmount;
        }

        /*
         * 每转过指定角度，触发一次棘轮震动。
         */
        if (reelTickTimer > 0f)
        {
            lowFrequency =
                Mathf.Max(
                    lowFrequency,
                    reelTickLowFrequency
                );

            highFrequency =
                Mathf.Max(
                    highFrequency,
                    reelTickHighFrequency
                );
        }

        /*
         * 发射或回收的动作震动。
         */
        if (actionRumbleTimer > 0f)
        {
            lowFrequency =
                Mathf.Max(
                    lowFrequency,
                    actionRumbleLowFrequency
                );

            highFrequency =
                Mathf.Max(
                    highFrequency,
                    actionRumbleHighFrequency
                );
        }

        ApplyRumble(
            gamepad,
            Mathf.Clamp01(lowFrequency),
            Mathf.Clamp01(highFrequency)
        );
    }

    private void ApplyRumble(
        Gamepad gamepad,
        float lowFrequency,
        float highFrequency)
    {
        /*
         * 如果中途切换了手柄，
         * 先停止旧手柄震动。
         */
        if (rumbleGamepad != null &&
            rumbleGamepad != gamepad)
        {
            rumbleGamepad.SetMotorSpeeds(
                0f,
                0f
            );
        }

        rumbleGamepad = gamepad;

        if (gamepad == null)
        {
            lastLowFrequency = -1f;
            lastHighFrequency = -1f;
            return;
        }

        /*
         * 变化非常小时不重复设置，
         * 减少不必要的手柄调用。
         */
        if (Mathf.Abs(
                lastLowFrequency -
                lowFrequency
            ) < 0.005f &&
            Mathf.Abs(
                lastHighFrequency -
                highFrequency
            ) < 0.005f)
        {
            return;
        }

        gamepad.SetMotorSpeeds(
            lowFrequency,
            highFrequency
        );

        lastLowFrequency =
            lowFrequency;

        lastHighFrequency =
            highFrequency;
    }

    private void StopRumble()
    {
        if (rumbleGamepad != null)
        {
            rumbleGamepad.SetMotorSpeeds(
                0f,
                0f
            );
        }

        rumbleGamepad = null;

        lastLowFrequency = -1f;
        lastHighFrequency = -1f;

        reelTickTimer = 0f;
        actionRumbleTimer = 0f;
    }

    private void ResetReelTracking()
    {
        hasPreviousReelAngle = false;
        previousReelAngle = 0f;

        currentReelAmount = 0f;
        reelGraceTimer = 0f;
        accumulatedReelDegrees = 0f;

        reelTickTimer = 0f;
    }

    /// <summary>
    /// 供多人输入管理器指定手柄编号。
    /// </summary>
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