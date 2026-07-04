using UnityEngine;

public partial class PlayerController
{

    private void UpdateGroundedState()
    {
        if (isClimbing)
        {
            isGrounded = false;
            currentGroundCollider = null;
            return;
        }

        Vector2 checkPosition =
            GetGroundCheckPosition();

        currentGroundCollider =
            Physics2D.OverlapCircle(
                checkPosition,
                groundCheckRadius,
                groundLayer
            );

        isGrounded =
            currentGroundCollider != null;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (groundCheck != null)
        {
            return groundCheck.position;
        }

        if (bodyCollider != null &&
            bodyCollider.enabled)
        {
            return new Vector2(
                bodyCollider.bounds.center.x,
                bodyCollider.bounds.min.y
            );
        }

        return rb.position;
    }

    private bool HasPhysicalGroundContact()
    {
        if (bodyCollider == null ||
            !bodyCollider.enabled ||
            currentGroundCollider == null)
        {
            return false;
        }

        return bodyCollider.IsTouching(
            currentGroundCollider
        );
    }


    private Vector2 GetColliderMotionVelocity(
        Collider2D targetCollider)
    {
        if (targetCollider == null)
        {
            return Vector2.zero;
        }

        SubmarineInteriorFollower2D follower =
            targetCollider
                .GetComponentInParent
                <SubmarineInteriorFollower2D>();

        if (follower != null)
        {
            return follower.GetVelocityAtPoint(
                rb.position
            );
        }

        Rigidbody2D targetRigidbody =
            targetCollider.attachedRigidbody;

        if (targetRigidbody != null)
        {
            return targetRigidbody
                .GetPointVelocity(
                    rb.position
                );
        }

        return Vector2.zero;
    }
}
