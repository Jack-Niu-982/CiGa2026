using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameplayPlayerStatusBarView : MonoBehaviour
{
    [SerializeField]
    private GameplayPlayerStatusSlotView[] slotViews =
        new GameplayPlayerStatusSlotView[RoomInputManager.MaxPlayers];

    private readonly List<PlayerCarryInteractor2D> subscribedCarryInteractors =
        new List<PlayerCarryInteractor2D>(RoomInputManager.MaxPlayers);

    private void OnEnable()
    {
        GameplayPlayerRegistry.Changed += HandlePlayersChanged;
        HandlePlayersChanged();
    }

    private void OnDisable()
    {
        GameplayPlayerRegistry.Changed -= HandlePlayersChanged;
        UnsubscribeCarryInteractors();
    }

    private void HandlePlayersChanged()
    {
        RebindCarryInteractors();
        Refresh();
    }

    private void RebindCarryInteractors()
    {
        UnsubscribeCarryInteractors();

        IReadOnlyList<GameplayPlayerIdentity> players =
            GameplayPlayerRegistry.Players;

        for (int i = 0; i < players.Count; i++)
        {
            PlayerCarryInteractor2D carryInteractor =
                players[i] != null
                    ? players[i].GetComponent<PlayerCarryInteractor2D>()
                    : null;

            if (carryInteractor == null)
            {
                continue;
            }

            carryInteractor.HeldItemChanged +=
                HandleHeldItemChanged;

            subscribedCarryInteractors.Add(carryInteractor);
        }
    }

    private void UnsubscribeCarryInteractors()
    {
        for (int i = 0; i < subscribedCarryInteractors.Count; i++)
        {
            if (subscribedCarryInteractors[i] != null)
            {
                subscribedCarryInteractors[i].HeldItemChanged -=
                    HandleHeldItemChanged;
            }
        }

        subscribedCarryInteractors.Clear();
    }

    private void HandleHeldItemChanged(
        PlayerCarryInteractor2D carryInteractor,
        CarryableItem2D item)
    {
        Refresh();
    }

    public void Refresh()
    {
        IReadOnlyList<GameplayPlayerIdentity> players =
            GameplayPlayerRegistry.Players;

        for (int i = 0; i < slotViews.Length; i++)
        {
            GameplayPlayerStatusSlotView slotView =
                slotViews[i];

            if (slotView == null)
            {
                continue;
            }

            GameplayPlayerIdentity player =
                i < players.Count
                    ? players[i]
                    : null;

            slotView.Render(player);
        }
    }
}
