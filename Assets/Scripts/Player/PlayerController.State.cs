using UnityEngine;

public partial class PlayerController
{

    private void UpdateMovementState()
    {
        if (playerInput == null)
        {
            return;
        }

        if (isOperating)
        {
            HandleOperatingStateInput();
        }
        else if (isClimbing)
        {
            HandleClimbingStateInput();
        }
        else
        {
            HandleNormalStateInput();
        }
    }

    private void HandleOperatingStateInput()
    {
        /*
         * 操作台被关闭、删除或强制退出时，
         * 自动恢复玩家正常状态。
         */
        if (activeOperateController == null ||
            !activeOperateController.IfInUse)
        {
            ExitOperatingState(
                activeOperateController
            );

            return;
        }

        /*
         * 交互状态下不在这里处理方向输入。
         *
         * 左右和上下输入仍然可以被锚发射器等设施读取，
         * 但不会用于玩家自身移动。
         *
         * 退出交互由PlayerOperateInteractor2D
         * 检测交互键完成。
         */
    }

    private void HandleNormalStateInput()
    {
        // 唯一新增的主要逻辑：
        // 梯子附近按上或按下都能进入攀爬。
        if (detectedLadder != null &&
            (
                playerInput.UpPressed ||
                playerInput.DownPressed
            ))
        {
            EnterClimbing();
            return;
        }

        // 没有进入梯子时，只有按上才会跳跃。
        if (isGrounded &&
            playerInput.UpPressed)
        {
            QueueJump();
        }
    }

    private void HandleClimbingStateInput()
    {
        if (activeLadder == null)
        {
            ExitClimbing();
            return;
        }

        HandleHorizontalLadderExit();

        if (!isClimbing)
        {
            return;
        }

        bool ignoredSurfaceStillDetected;

        Collider2D landingSurface =
            DetectClosestLandingSurface(
                ignoredEntryInteriorSurface,
                out ignoredSurfaceStillDetected
            );

        if (ignoredEntryInteriorSurface != null)
        {
            if (ignoredSurfaceStillDetected)
            {
                return;
            }

            ignoredEntryInteriorSurface = null;
        }

        if (landingSurface != null)
        {
            LandOnInteriorSurface(
                landingSurface
            );
        }
    }

    private void HandleHorizontalLadderExit()
    {
        PlayerSettings settings =
            SettingManager.Player;

        if (settings == null)
        {
            return;
        }

        if (Mathf.Abs(horizontalInput) >=
            settings.ladderExitInputThreshold)
        {
            ladderExitTimer +=
                Time.deltaTime;

            if (ladderExitTimer >=
                settings.ladderExitHoldTime)
            {
                ExitClimbing();
            }
        }
        else
        {
            ladderExitTimer = 0f;
        }
    }


    private void ApplyNormalMovement()
    {
        PlayerSettings settings =
            SettingManager.Player;

        if (settings == null)
        {
            return;
        }

        Vector2 platformVelocity =
            Vector2.zero;

        if (isGrounded)
        {
            platformVelocity =
                GetColliderMotionVelocity(
                    currentGroundCollider
                );
        }

        Vector2 velocity =
            rb.velocity;

        velocity.x =
            platformVelocity.x +
            horizontalInput * settings.moveSpeed;

        if (jumpQueued)
        {
            velocity.y =
                platformVelocity.y +
                settings.jumpForce;

            jumpQueued = false;
            isGrounded = false;
            currentGroundCollider = null;

            SetAnimatorTrigger(
                jumpTriggerParameter
            );

            OnJumped();
        }
        else if (
            isGrounded &&
            HasPhysicalGroundContact()
        )
        {
            velocity.y =
                platformVelocity.y;
        }

        rb.velocity = velocity;
    }

    private void QueueJump()
    {
        jumpQueued = true;
    }


    /// <summary>
    /// 由PlayerOperateInteractor2D在成功开始交互时调用。
    /// </summary>
    public void EnterOperatingState(
        OperateController operateController)
    {
        if (operateController == null)
        {
            return;
        }

        if (isClimbing)
        {
            ExitClimbing();
        }

        activeOperateController =
            operateController;

        isOperating = true;
        jumpQueued = false;

        detectedLadder = null;

        horizontalInput = 0f;
        verticalInput = 0f;

        OnEnteredOperating();
    }

    /// <summary>
    /// 由PlayerOperateInteractor2D在交互结束时调用。
    /// </summary>
    public void ExitOperatingState(
        OperateController operateController)
    {
        if (!isOperating)
        {
            return;
        }

        if (operateController != null &&
            activeOperateController != null &&
            operateController !=
            activeOperateController)
        {
            return;
        }

        isOperating = false;
        activeOperateController = null;

        horizontalInput = 0f;
        verticalInput = 0f;

        OnExitedOperating();
    }

    /// <summary>
    /// 交互状态下不响应任何方向移动。
    /// 仍然继承脚下平台的移动速度，避免潜艇移动时玩家滑开。
    /// </summary>
    private void ApplyOperatingMovement()
    {
        Vector2 velocity =
            rb.velocity;

        if (isGrounded)
        {
            Vector2 platformVelocity =
                GetColliderMotionVelocity(
                    currentGroundCollider
                );

            velocity.x =
                platformVelocity.x;

            if (HasPhysicalGroundContact())
            {
                velocity.y =
                    platformVelocity.y;
            }
        }
        else
        {
            velocity.x = 0f;
        }

        rb.velocity = velocity;
    }


    public void SetPlayerInput(
        PlayerInputBase newInput)
    {
        playerInput = newInput;
    }
}
