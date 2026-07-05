using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class PlayerAnimatorControllerSelector : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Tooltip("按 Cat1、Cat2、Cat3、Cat4 的顺序配置 Animator Controller。")]
    [SerializeField] private RuntimeAnimatorController[] playerControllers =
        new RuntimeAnimatorController[RoomInputManager.MaxPlayers];

    [Tooltip("按 Cat1、Cat2、Cat3、Cat4 的顺序配置 HUD 头像。")]
    [SerializeField] private Sprite[] portraitSprites =
        new Sprite[RoomInputManager.MaxPlayers];

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void Configure(int playerIndex)
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null ||
            playerControllers == null ||
            playerIndex < 0 ||
            playerIndex >= playerControllers.Length)
        {
            return;
        }

        RuntimeAnimatorController controller =
            playerControllers[playerIndex];

        if (controller == null ||
            animator.runtimeAnimatorController == controller)
        {
            return;
        }

        animator.runtimeAnimatorController = controller;
        animator.Rebind();
        animator.Update(0f);
    }

    public Sprite GetPortraitSprite(int characterIndex)
    {
        if (portraitSprites == null ||
            characterIndex < 0 ||
            characterIndex >= portraitSprites.Length)
        {
            return null;
        }

        return portraitSprites[characterIndex];
    }
}
