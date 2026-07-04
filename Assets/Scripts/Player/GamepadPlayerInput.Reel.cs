using UnityEngine;
using UnityEngine.InputSystem;

public partial class GamepadPlayerInput
{
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
}
