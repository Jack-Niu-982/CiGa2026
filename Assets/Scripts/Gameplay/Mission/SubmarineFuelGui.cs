using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SubmarineFuelGui : MonoBehaviour
{
    [SerializeField]
    private SubmarineFuel2D fuel;

    [SerializeField]
    private Scrollbar fuelScrollbar;

    private void Awake()
    {
        ResolveReferences();
        ConfigureScrollbar();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (fuel != null)
        {
            fuel.FuelChanged += HandleFuelChanged;
        }

        ConfigureScrollbar();
        Refresh();
    }

    private void OnDisable()
    {
        if (fuel != null)
        {
            fuel.FuelChanged -= HandleFuelChanged;
        }
    }

    private void OnValidate()
    {
        if (fuelScrollbar == null)
        {
            fuelScrollbar = GetComponent<Scrollbar>();
        }

        ConfigureScrollbar();
    }

    private void ResolveReferences()
    {
        if (fuelScrollbar == null)
        {
            fuelScrollbar = GetComponent<Scrollbar>();
        }

        if (fuel == null)
        {
            fuel = FindObjectOfType<SubmarineFuel2D>();
        }
    }

    private void ConfigureScrollbar()
    {
        if (fuelScrollbar == null)
        {
            return;
        }

        fuelScrollbar.interactable = false;
        fuelScrollbar.direction =
            Scrollbar.Direction.LeftToRight;
        fuelScrollbar.numberOfSteps = 0;
        fuelScrollbar.value = 0f;
    }

    private void HandleFuelChanged(
        float currentFuel,
        float maxFuel)
    {
        SetFuelRatio(currentFuel, maxFuel);
    }

    private void Refresh()
    {
        SetFuelRatio(
            fuel != null ? fuel.CurrentFuel : 0f,
            fuel != null ? fuel.MaxFuel : 1f
        );
    }

    private void SetFuelRatio(
        float currentFuel,
        float maxFuel)
    {
        if (fuelScrollbar == null)
        {
            return;
        }

        fuelScrollbar.size =
            maxFuel > 0f
                ? Mathf.Clamp01(
                    currentFuel / maxFuel
                )
                : 0f;
    }
}
