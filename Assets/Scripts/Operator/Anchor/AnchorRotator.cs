using UnityEngine;

/// <summary>
/// 控制船锚发射器偏转。
///
/// 只有对应 OperateController 的 IfInUse 为 true 时才会工作。
/// 输入只读取当前操作玩家自己的 PlayerInputBase。
///
/// 当前朝向偏上或偏下：
/// 使用玩家的左右输入控制偏转。
///
/// 当前朝向偏左或偏右：
/// 使用玩家的上下输入控制偏转。
///
/// 基础方向直接读取 AnchorLauncher2D.Direction，
/// 不再需要单独配置 AnchorReference。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class AnchorRotator : MonoBehaviour
{
    [Header("组件引用")]

    [Tooltip(
        "对应的 AnchorLauncher2D。" +
        "留空时会自动从当前物体或父物体寻找。"
    )]
    [SerializeField]
    private AnchorLauncher2D anchorLauncher;

    [Tooltip(
        "负责该发射器交互状态的 OperateController。" +
        "留空时会自动从父物体寻找。"
    )]
    [SerializeField]
    private OperateController operateController;

    [Tooltip("锚点转向音效。留空时自动读取当前物体上的组件。")]
    [SerializeField]
    private AnchorAudioFeedback2D audioFeedback;

    [Header("旋转设置")]

    [Tooltip("玩家按住方向键时，每秒旋转的角度。")]
    [Min(0f)]
    [SerializeField]
    private float rotationSpeed = 90f;

    [Tooltip("相对于初始方向允许偏转的最大角度。")]
    [Range(0f, 180f)]
    [SerializeField]
    private float maxRotationAngle = 45f;

    [Tooltip("输入小于该数值时不进行旋转。")]
    [Range(0f, 1f)]
    [SerializeField]
    private float inputDeadZone = 0.1f;

    [Tooltip("反转操作方向。")]
    [SerializeField]
    private bool invertControl;

    [Header("调试")]

    [SerializeField]
    private bool showDebugLog = true;

    /// <summary>
    /// 发射器场景开始时的本地旋转。
    /// </summary>
    private Quaternion initialLocalRotation;

    /// <summary>
    /// 当前相对于初始方向的偏转角度。
    /// </summary>
    private float currentRotationAngle;

    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        /*
         * AnchorRotator 可能会被 OperateController
         * 在开始操作时重新启用。
         *
         * 重新启用后同步当前真实角度，
         * 防止发射器突然跳回初始角度。
         */
        if (initialized)
        {
            SynchronizeCurrentAngle();
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        /*
         * 只有对应设备正在被玩家操作时，
         * 才允许读取方向输入。
         */
        if (operateController == null ||
            !operateController.IfInUse)
        {
            return;
        }

        PlayerInputBase playerInput =
            GetCurrentOperatorInput();

        if (playerInput == null)
        {
            return;
        }

        HandleRotationInput(playerInput);
    }

    /// <summary>
    /// 初始化组件引用和初始旋转。
    /// </summary>
    private void Initialize()
    {
        initialLocalRotation =
            transform.localRotation;

        FindReferences();

        if (anchorLauncher == null)
        {
            Debug.LogError(
                $"[AnchorRotator] {gameObject.name} " +
                "没有找到 AnchorLauncher2D。"
            );

            enabled = false;
            return;
        }

        if (operateController == null)
        {
            Debug.LogError(
                $"[AnchorRotator] {gameObject.name} " +
                "没有找到 OperateController。"
            );

            enabled = false;
            return;
        }

        currentRotationAngle = 0f;
        initialized = true;

        if (showDebugLog)
        {
            Debug.Log(
                $"[AnchorRotator] {gameObject.name} 初始化完成。\n" +
                $"基础方向：{anchorLauncher.Direction}\n" +
                $"最大偏转：±{maxRotationAngle}°"
            );
        }
    }

    /// <summary>
    /// 自动寻找组件引用。
    /// </summary>
    private void FindReferences()
    {
        if (anchorLauncher == null)
        {
            anchorLauncher =
                GetComponent<AnchorLauncher2D>();
        }

        if (anchorLauncher == null)
        {
            anchorLauncher =
                GetComponentInParent<AnchorLauncher2D>();
        }

        if (operateController == null)
        {
            operateController =
                GetComponentInParent<OperateController>();
        }

        if (audioFeedback == null)
        {
            audioFeedback =
                GetComponent<AnchorAudioFeedback2D>();
        }
    }

    /// <summary>
    /// 获取当前正在操作该设备的玩家自己的输入组件。
    ///
    /// 正确路径：
    /// OperateController
    /// → CurrentOperatorInteractor
    /// → PlayerObject
    /// → PlayerController
    /// → CurrentInput
    /// </summary>
    private PlayerInputBase GetCurrentOperatorInput()
    {
        if (operateController == null ||
            !operateController.IfInUse)
        {
            return null;
        }

        PlayerOperateInteractor2D operatorInteractor =
            operateController.CurrentOperatorInteractor;

        if (operatorInteractor == null)
        {
            return null;
        }

        GameObject playerObject =
            operatorInteractor.PlayerObject;

        if (playerObject == null)
        {
            return null;
        }

        /*
         * 优先读取 PlayerController 中实际绑定的输入组件。
         *
         * 这样键盘玩家会取得 KeyboardPlayerInput，
         * 手柄玩家会取得属于自己的 GamepadPlayerInput。
         */
        PlayerController playerController =
            playerObject.GetComponent<PlayerController>();

        if (playerController == null)
        {
            playerController =
                playerObject.GetComponentInParent<PlayerController>();
        }

        if (playerController == null)
        {
            playerController =
                playerObject.GetComponentInChildren<PlayerController>(true);
        }

        if (playerController != null &&
            playerController.CurrentInput != null)
        {
            return playerController.CurrentInput;
        }

        /*
         * 备用寻找方式。
         * 即使玩家没有 PlayerController，
         * 只要存在 PlayerInputBase 也可以正常读取。
         */
        PlayerInputBase playerInput =
            playerObject.GetComponent<PlayerInputBase>();

        if (playerInput == null)
        {
            playerInput =
                playerObject.GetComponentInParent<PlayerInputBase>();
        }

        if (playerInput == null)
        {
            playerInput =
                playerObject.GetComponentInChildren<PlayerInputBase>(true);
        }

        if (playerInput == null && showDebugLog)
        {
            Debug.LogWarning(
                $"[AnchorRotator] 当前操作玩家 " +
                $"{playerObject.name} 上没有找到 PlayerInputBase。"
            );
        }

        return playerInput;
    }

    /// <summary>
    /// 根据发射器配置方向选择当前操作玩家的输入轴。
    ///
    /// Up / Down：使用左右输入。
    /// Left / Right：使用上下输入。
    /// </summary>
    private void HandleRotationInput(
        PlayerInputBase playerInput)
    {
        float inputValue;
        float rotationSign;

        switch (anchorLauncher.Direction)
        {
            /*
             * 上方发射器：
             * 左右控制偏转。
             *
             * 按右时顺时针旋转，
             * 因此使用负角度。
             */
            case AnchorLauncher2D.AnchorDirection.Up:
                inputValue =
                    playerInput.Vertical;

                rotationSign = -1f;
                break;

            /*
             * 下方发射器：
             * 左右控制偏转。
             *
             * 按右时逆时针旋转，
             * 因此使用正角度。
             */
            case AnchorLauncher2D.AnchorDirection.Down:
                inputValue =
                    playerInput.Vertical;

                rotationSign = 1f;
                break;

            /*
             * 右侧发射器：
             * 上下控制偏转。
             *
             * 按上时逆时针旋转，
             * 因此使用正角度。
             */
            case AnchorLauncher2D.AnchorDirection.Right:
                inputValue =
                    playerInput.Vertical;

                rotationSign = 1f;
                break;

            /*
             * 左侧发射器：
             * 上下控制偏转。
             *
             * 按上时顺时针旋转，
             * 因此使用负角度。
             */
            case AnchorLauncher2D.AnchorDirection.Left:
                inputValue =
                    playerInput.Vertical;

                rotationSign = -1f;
                break;

            default:
                return;
        }

        if (Mathf.Abs(inputValue) <
            inputDeadZone)
        {
            return;
        }

        if (invertControl)
        {
            rotationSign *= -1f;
        }

        currentRotationAngle +=
            inputValue *
            rotationSign *
            rotationSpeed *
            Time.deltaTime;

        currentRotationAngle =
            Mathf.Clamp(
                currentRotationAngle,
                -maxRotationAngle,
                maxRotationAngle
            );

        ApplyCurrentRotation();

        if (audioFeedback != null)
        {
            audioFeedback.PlayRotate();
        }
    }

    /// <summary>
    /// 获取发射器未偏转状态下的当前世界朝向。
    ///
    /// 会跟随潜艇和父物体旋转，
    /// 但不会因为船锚自身偏转而频繁切换控制轴。
    /// </summary>
    private Vector2 GetBaseWorldDirection()
    {
        Vector2 configuredDirection =
            GetConfiguredDirection(
                anchorLauncher.Direction
            );

        Vector3 directionInParentSpace =
            initialLocalRotation *
            new Vector3(
                configuredDirection.x,
                configuredDirection.y,
                0f
            );

        Vector3 worldDirection;

        if (transform.parent != null)
        {
            worldDirection =
                transform.parent.TransformDirection(
                    directionInParentSpace
                );
        }
        else
        {
            worldDirection =
                directionInParentSpace;
        }

        Vector2 result =
            new Vector2(
                worldDirection.x,
                worldDirection.y
            );

        if (result.sqrMagnitude >
            0.0001f)
        {
            result.Normalize();
        }

        return result;
    }

    /// <summary>
    /// 将 AnchorLauncher2D 的配置方向转换为方向向量。
    /// </summary>
    private Vector2 GetConfiguredDirection(
        AnchorLauncher2D.AnchorDirection direction)
    {
        switch (direction)
        {
            case AnchorLauncher2D.AnchorDirection.Up:
                return Vector2.up;

            case AnchorLauncher2D.AnchorDirection.Right:
                return Vector2.right;

            case AnchorLauncher2D.AnchorDirection.Down:
                return Vector2.down;

            case AnchorLauncher2D.AnchorDirection.Left:
                return Vector2.left;

            default:
                return Vector2.up;
        }
    }

    /// <summary>
    /// 应用当前偏转角度。
    /// </summary>
    private void ApplyCurrentRotation()
    {
        transform.localRotation =
            initialLocalRotation *
            Quaternion.Euler(
                0f,
                0f,
                currentRotationAngle
            );
    }

    /// <summary>
    /// 组件重新启用时，
    /// 根据当前实际旋转同步内部偏转角度。
    /// </summary>
    private void SynchronizeCurrentAngle()
    {
        Quaternion relativeRotation =
            Quaternion.Inverse(
                initialLocalRotation
            ) *
            transform.localRotation;

        currentRotationAngle =
            Mathf.DeltaAngle(
                0f,
                relativeRotation.eulerAngles.z
            );

        currentRotationAngle =
            Mathf.Clamp(
                currentRotationAngle,
                -maxRotationAngle,
                maxRotationAngle
            );

        ApplyCurrentRotation();
    }

    /// <summary>
    /// 将发射器恢复到初始朝向。
    /// </summary>
    public void ResetRotation()
    {
        currentRotationAngle = 0f;

        transform.localRotation =
            initialLocalRotation;
    }

    /// <summary>
    /// 获取当前偏转角度。
    /// </summary>
    public float GetCurrentRotationAngle()
    {
        return currentRotationAngle;
    }

    private void OnValidate()
    {
        rotationSpeed =
            Mathf.Max(
                0f,
                rotationSpeed
            );

        maxRotationAngle =
            Mathf.Clamp(
                maxRotationAngle,
                0f,
                180f
            );

        inputDeadZone =
            Mathf.Clamp01(
                inputDeadZone
            );

        if (!Application.isPlaying)
        {
            FindReferences();
        }
    }
}
