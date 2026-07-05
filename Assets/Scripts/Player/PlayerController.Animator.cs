using UnityEngine;

public partial class PlayerController
{

    private void CacheAnimatorParameters()
    {
        animatorParameterHashes.Clear();

        if (animator == null)
        {
            return;
        }

        foreach (
            AnimatorControllerParameter parameter
            in animator.parameters)
        {
            animatorParameterHashes.Add(
                parameter.nameHash
            );
        }
    }

    private void UpdateAnimator()
    {
        UpdateFacingDirection();

        if (animator == null)
        {
            return;
        }

        float animatorHorizontalInput =
            isGrounded &&
            !isClimbing &&
            !isOperating
                ? horizontalInput
                : 0f;

        SetAnimatorFloat(
            moveXParameter,
            animatorHorizontalInput
        );

        SetAnimatorFloat(
            moveSpeedParameter,
            Mathf.Abs(
                animatorHorizontalInput
            )
        );

        SetAnimatorFloat(
            verticalSpeedParameter,
            rb.velocity.y
        );

        SetAnimatorBool(
            groundedParameter,
            isGrounded &&
            !isClimbing
        );

        SetAnimatorBool(
            climbingParameter,
            isClimbing
        );

        SetAnimatorBool(
            operatingParameter,
            isOperating
        );
    }

    private void UpdateFacingDirection()
    {
        if (spriteRenderer == null ||
            isOperating ||
            Mathf.Abs(horizontalInput) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX =
            horizontalInput < 0f;
    }

    private void SetAnimatorFloat(
        string parameterName,
        float value)
    {
        if (!TryGetAnimatorParameterHash(
                parameterName,
                out int hash))
        {
            return;
        }

        animator.SetFloat(
            hash,
            value
        );
    }

    private void SetAnimatorBool(
        string parameterName,
        bool value)
    {
        if (!TryGetAnimatorParameterHash(
                parameterName,
                out int hash))
        {
            return;
        }

        animator.SetBool(
            hash,
            value
        );
    }

    private void SetAnimatorTrigger(
        string parameterName)
    {
        if (!TryGetAnimatorParameterHash(
                parameterName,
                out int hash))
        {
            return;
        }

        animator.SetTrigger(hash);
    }

    private bool TryGetAnimatorParameterHash(
        string parameterName,
        out int hash)
    {
        hash = 0;

        if (animator == null ||
            string.IsNullOrWhiteSpace(
                parameterName))
        {
            return false;
        }

        hash =
            Animator.StringToHash(
                parameterName
            );

        return animatorParameterHashes.Contains(
            hash
        );
    }


    protected virtual void OnJumped()
    {
    }

    protected virtual void OnEnteredClimbing()
    {
    }

    protected virtual void OnExitedClimbing()
    {
    }

    protected virtual void OnEnteredOperating()
    {
    }

    protected virtual void OnExitedOperating()
    {
    }
}
