using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(10)]
[DisallowMultipleComponent]
public class PlayerPutInInteractor2D : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerInputBase fallbackInput;

    [SerializeField]
    private PlayerCarryInteractor2D carryInteractor;

    private readonly HashSet<CarryItemDepositPoint2D>
        nearbyDepositPoints =
            new HashSet<CarryItemDepositPoint2D>();

    public bool HasPutInOption =>
        GetClosestAvailablePoint() != null;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (fallbackInput == null)
        {
            fallbackInput = GetComponent<PlayerInputBase>();
        }

        if (carryInteractor == null)
        {
            carryInteractor =
                GetComponent<PlayerCarryInteractor2D>();
        }
    }

    private void Update()
    {
        PlayerInputBase input = GetCurrentInput();

        if (input == null ||
            !input.PutInPressed)
        {
            return;
        }

        CarryItemDepositPoint2D point =
            GetClosestAvailablePoint();

        if (point != null &&
            carryInteractor != null)
        {
            carryInteractor.TryConsumeHeldItem(point);
        }
    }

    public void RegisterDepositPoint(
        CarryItemDepositPoint2D point)
    {
        if (point != null)
        {
            nearbyDepositPoints.Add(point);
        }
    }

    public void UnregisterDepositPoint(
        CarryItemDepositPoint2D point)
    {
        if (point != null)
        {
            nearbyDepositPoints.Remove(point);
        }
    }

    private CarryItemDepositPoint2D
        GetClosestAvailablePoint()
    {
        nearbyDepositPoints.RemoveWhere(
            point => point == null
        );

        if (carryInteractor == null ||
            carryInteractor.HeldItem == null)
        {
            return null;
        }

        CarryItemDepositPoint2D closest = null;
        float closestDistance = float.MaxValue;

        foreach (CarryItemDepositPoint2D point
                 in nearbyDepositPoints)
        {
            if (!point.CanReceiveCarryItem(
                    carryInteractor,
                    carryInteractor.HeldItem
                ))
            {
                continue;
            }

            float distance =
                (point.transform.position -
                 transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closest = point;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private PlayerInputBase GetCurrentInput()
    {
        return
            playerController != null &&
            playerController.CurrentInput != null
                ? playerController.CurrentInput
                : fallbackInput;
    }

    private void OnDisable()
    {
        nearbyDepositPoints.Clear();
    }
}
