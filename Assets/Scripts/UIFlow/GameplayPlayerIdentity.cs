using UnityEngine;

[DisallowMultipleComponent]
public class GameplayPlayerIdentity : MonoBehaviour
{
    [SerializeField]
    private int playerIndex;

    [SerializeField]
    private Sprite portraitSprite;

    public int PlayerIndex => playerIndex;
    public string PlayerLabel => $"P{playerIndex + 1}";
    public Sprite PortraitSprite => portraitSprite;

    public void Configure(
        int newPlayerIndex,
        Sprite newPortraitSprite = null)
    {
        playerIndex =
            Mathf.Clamp(
                newPlayerIndex,
                0,
                RoomInputManager.MaxPlayers - 1
            );

        if (newPortraitSprite != null)
        {
            portraitSprite = newPortraitSprite;
        }
    }
}
