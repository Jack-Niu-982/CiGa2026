using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ChooseCharacterCardView : MonoBehaviour
{
    [SerializeField] private Image frameImage;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text characterNameLabel;
    [SerializeField] private TMP_Text hoverLabel;
    [SerializeField] private TMP_Text confirmedLabel;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] rotateFrames;
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private Color normalColor =
        new Color(0.12f, 0.18f, 0.28f, 0.96f);
    [SerializeField] private Color highlightedColor =
        new Color(0.18f, 0.58f, 0.78f, 1f);
    [SerializeField] private Color confirmedColor =
        new Color(0.20f, 0.72f, 0.42f, 1f);

    private bool isAnimating;
    private float animationTime;

    public void SetCharacterName(string characterName)
    {
        if (characterNameLabel != null)
        {
            characterNameLabel.text = characterName;
        }
    }

    public void Render(
        string hoveringPlayers,
        string confirmedPlayer)
    {
        bool hasHover =
            !string.IsNullOrWhiteSpace(hoveringPlayers);
        bool isConfirmed =
            !string.IsNullOrWhiteSpace(confirmedPlayer);

        if (hoverLabel != null)
        {
            hoverLabel.text = hasHover
                ? hoveringPlayers
                : string.Empty;
        }

        if (confirmedLabel != null)
        {
            confirmedLabel.text = isConfirmed
                ? confirmedPlayer + " READY"
                : string.Empty;
        }

        if (frameImage != null)
        {
            frameImage.color = isConfirmed
                ? confirmedColor
                : hasHover
                    ? highlightedColor
                    : normalColor;
        }

        SetAnimating(hasHover || isConfirmed);
    }

    private void Update()
    {
        if (!isAnimating ||
            portraitImage == null ||
            rotateFrames == null ||
            rotateFrames.Length == 0)
        {
            return;
        }

        animationTime += Time.unscaledDeltaTime;

        int frameIndex =
            Mathf.FloorToInt(
                animationTime * Mathf.Max(1f, framesPerSecond)
            ) % rotateFrames.Length;

        portraitImage.sprite = rotateFrames[frameIndex];
    }

    private void SetAnimating(bool shouldAnimate)
    {
        if (isAnimating == shouldAnimate)
        {
            return;
        }

        isAnimating = shouldAnimate;
        animationTime = 0f;

        if (!isAnimating && portraitImage != null)
        {
            portraitImage.sprite = idleSprite;
        }
    }
}
