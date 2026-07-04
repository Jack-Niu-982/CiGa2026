using UnityEngine;
using UnityEngine.InputSystem;

public partial class GamepadPlayerInput
{
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
}
