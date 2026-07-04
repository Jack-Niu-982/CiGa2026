using UnityEngine;

public partial class PlayerController
{

    private void UpdateNearbyLadder()
    {
        Vector2 checkPosition =
            ladderCheck != null
                ? ladderCheck.position
                : transform.position;

        float checkAngle =
            ladderCheck != null
                ? ladderCheck.eulerAngles.z
                : transform.eulerAngles.z;

        int resultCount =
            Physics2D.OverlapBoxNonAlloc(
                checkPosition,
                ladderCheckSize,
                checkAngle,
                ladderResults,
                ladderLayer
            );

        if (resultCount <= 0)
        {
            detectedLadder = null;
            return;
        }

        Collider2D closestLadder = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < resultCount; i++)
        {
            Collider2D ladder =
                ladderResults[i];

            if (ladder == null)
            {
                continue;
            }

            Vector2 closestPoint =
                ladder.ClosestPoint(
                    rb.position
                );

            float distance =
                Vector2.SqrMagnitude(
                    closestPoint -
                    rb.position
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLadder = ladder;
            }
        }

        detectedLadder = closestLadder;
    }


    private void ApplyClimbingMovement()
    {
        if (activeLadder == null)
        {
            ExitClimbing();
            return;
        }

        if (!TryGetLadderGeometry(
                activeLadder,
                out Vector2 ladderCenter,
                out Vector2 ladderUp,
                out Vector2 ladderRight,
                out float ladderHalfLength))
        {
            rb.velocity =
                GetColliderMotionVelocity(
                    activeLadder
                );

            return;
        }

        Vector2 ladderVelocity =
            GetColliderMotionVelocity(
                activeLadder
            );

        Vector2 playerBodyCenter =
            GetPlayerBodyCenter();

        Vector2 centerDifference =
            playerBodyCenter -
            ladderCenter;

        float currentVerticalDistance =
            Vector2.Dot(
                centerDifference,
                ladderUp
            );

        float currentHorizontalDistance =
            Vector2.Dot(
                centerDifference,
                ladderRight
            );

        float playerHalfExtentOnLadder =
            GetPlayerHalfExtentAlongAxis(
                ladderUp
            );

        float allowedHalfDistance =
            Mathf.Max(
                0f,
                ladderHalfLength -
                playerHalfExtentOnLadder -
                ladderEndPadding
            );

        float wantedClimbSpeed =
            verticalInput *
            climbSpeed;

        float wantedVerticalDistance =
            currentVerticalDistance +
            wantedClimbSpeed *
            Time.fixedDeltaTime;

        float clampedVerticalDistance =
            Mathf.Clamp(
                wantedVerticalDistance,
                -allowedHalfDistance,
                allowedHalfDistance
            );

        float actualClimbSpeed =
            (
                clampedVerticalDistance -
                currentVerticalDistance
            ) /
            Time.fixedDeltaTime;

        float horizontalCorrectionSpeed = 0f;

        if (snapToLadderCenter)
        {
            float nextHorizontalDistance =
                Mathf.MoveTowards(
                    currentHorizontalDistance,
                    0f,
                    ladderSnapSpeed *
                    Time.fixedDeltaTime
                );

            horizontalCorrectionSpeed =
                (
                    nextHorizontalDistance -
                    currentHorizontalDistance
                ) /
                Time.fixedDeltaTime;
        }

        Vector2 relativeClimbVelocity =
            ladderUp *
            actualClimbSpeed +
            ladderRight *
            horizontalCorrectionSpeed;

        rb.velocity =
            ladderVelocity +
            relativeClimbVelocity;
    }

    private Vector2 GetPlayerBodyCenter()
    {
        return rb.position +
               bodyCenterOffset;
    }

    private float GetPlayerHalfExtentAlongAxis(
        Vector2 axis)
    {
        axis = new Vector2(
            Mathf.Abs(axis.x),
            Mathf.Abs(axis.y)
        );

        return
            axis.x *
            cachedBodyBoundsExtents.x +
            axis.y *
            cachedBodyBoundsExtents.y;
    }

    private bool TryGetLadderGeometry(
        Collider2D ladder,
        out Vector2 center,
        out Vector2 up,
        out Vector2 right,
        out float halfLength)
    {
        center =
            ladder.bounds.center;

        up =
            ladder.transform.up.normalized;

        right =
            ladder.transform.right.normalized;

        halfLength = 0f;

        if (ladder is BoxCollider2D boxCollider)
        {
            center =
                boxCollider.transform
                    .TransformPoint(
                        boxCollider.offset
                    );

            float worldHeight =
                Mathf.Abs(
                    boxCollider.size.y *
                    boxCollider.transform
                        .lossyScale.y
                );

            halfLength =
                worldHeight * 0.5f;

            return halfLength > 0f;
        }

        Bounds ladderBounds =
            ladder.bounds;

        halfLength =
            Mathf.Abs(up.x) *
            ladderBounds.extents.x +
            Mathf.Abs(up.y) *
            ladderBounds.extents.y;

        return halfLength > 0f;
    }


    private Collider2D DetectClosestLandingSurface(
        Collider2D colliderToIgnore,
        out bool ignoredColliderWasDetected)
    {
        ignoredColliderWasDetected = false;

        Vector2 checkPosition =
            ladderLandingCheck != null
                ? ladderLandingCheck.position
                : GetGroundCheckPosition();

        float checkAngle =
            ladderLandingCheck != null
                ? ladderLandingCheck.eulerAngles.z
                : transform.eulerAngles.z;

        int resultCount =
            Physics2D.OverlapBoxNonAlloc(
                checkPosition,
                ladderLandingCheckSize,
                checkAngle,
                landingResults,
                submarineInteriorLayer
            );

        Collider2D closestSurface = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < resultCount; i++)
        {
            Collider2D surface =
                landingResults[i];

            if (surface == null)
            {
                continue;
            }

            if (surface == colliderToIgnore)
            {
                ignoredColliderWasDetected = true;
                continue;
            }

            Vector2 closestPoint =
                surface.ClosestPoint(
                    checkPosition
                );

            float distance =
                Vector2.SqrMagnitude(
                    closestPoint -
                    checkPosition
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSurface = surface;
            }
        }

        return closestSurface;
    }

    private void LandOnInteriorSurface(
        Collider2D landingSurface)
    {
        if (!isClimbing ||
            landingSurface == null)
        {
            return;
        }

        Vector2 surfaceVelocity =
            GetColliderMotionVelocity(
                landingSurface
            );

        isClimbing = false;
        isGrounded = true;

        jumpQueued = false;
        ladderExitTimer = 0f;

        rb.gravityScale =
            normalGravityScale;

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        activeLadder = null;
        detectedLadder = null;
        ignoredEntryInteriorSurface = null;

        currentGroundCollider =
            landingSurface;

        rb.velocity =
            surfaceVelocity;

        OnExitedClimbing();
    }


    private void EnterClimbing()
    {
        if (isClimbing ||
            isOperating ||
            detectedLadder == null)
        {
            return;
        }

        activeLadder =
            detectedLadder;

        isClimbing = true;
        isGrounded = false;

        jumpQueued = false;
        ladderExitTimer = 0f;

        currentGroundCollider = null;

        rb.gravityScale = 0f;

        rb.velocity =
            GetColliderMotionVelocity(
                activeLadder
            );

        if (disableBodyColliderWhileClimbing &&
            bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        bool unused;

        ignoredEntryInteriorSurface =
            DetectClosestLandingSurface(
                null,
                out unused
            );

        OnEnteredClimbing();
    }

    private void ExitClimbing()
    {
        if (!isClimbing)
        {
            return;
        }

        isClimbing = false;
        ladderExitTimer = 0f;

        rb.gravityScale =
            normalGravityScale;

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        activeLadder = null;
        detectedLadder = null;
        ignoredEntryInteriorSurface = null;

        OnExitedClimbing();
    }
}
