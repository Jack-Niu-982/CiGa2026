using UnityEngine;

public partial class PlayerController
{
    private void OnDisable()
    {
        if (rb != null)
        {
            rb.gravityScale =
                normalGravityScale;
        }

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        isClimbing = false;
        isOperating = false;
        isGrounded = false;
        jumpQueued = false;

        activeLadder = null;
        activeOperateController = null;
        detectedLadder = null;
        currentGroundCollider = null;
        ignoredEntryInteriorSurface = null;

        ladderExitTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        PlayerSettings settings =
            SettingManager.Player;

        if (settings == null)
        {
            return;
        }

        Vector3 groundPosition =
            groundCheck != null
                ? groundCheck.position
                : transform.position;

        Gizmos.DrawWireSphere(
            groundPosition,
            settings.groundCheckRadius
        );

        DrawWireBox(
            ladderCheck != null
                ? ladderCheck.position
                : transform.position,
            ladderCheck != null
                ? ladderCheck.eulerAngles.z
                : transform.eulerAngles.z,
            settings.ladderCheckSize
        );

        DrawWireBox(
            ladderLandingCheck != null
                ? ladderLandingCheck.position
                : groundPosition,
            ladderLandingCheck != null
                ? ladderLandingCheck.eulerAngles.z
                : transform.eulerAngles.z,
            settings.ladderLandingCheckSize
        );
    }

    private void DrawWireBox(
        Vector3 position,
        float angle,
        Vector2 size)
    {
        Matrix4x4 oldMatrix =
            Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                position,
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                ),
                Vector3.one
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            size
        );

        Gizmos.matrix =
            oldMatrix;
    }
}
