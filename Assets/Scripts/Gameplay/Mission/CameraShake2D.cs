using UnityEngine;

/// <summary>
/// 监听潜艇受伤事件并震动当前相机。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public class CameraShake2D : MonoBehaviour
{
    [Header("震动参数")]
    [SerializeField]
    private float duration = 0.18f;

    [SerializeField]
    private float amplitude = 0.14f;

    [SerializeField]
    private float frequency = 42f;

    [SerializeField]
    private float damageScale = 0.01f;

    private Vector3 previousOffset;
    private float remainingTime;
    private float currentDuration;
    private float currentAmplitude;
    private float seed;

    private void Awake()
    {
        seed =
            Random.value * 100f;
    }

    private void OnEnable()
    {
        GameplayEventBus.SubmarineDamaged +=
            HandleSubmarineDamaged;
    }

    private void OnDisable()
    {
        GameplayEventBus.SubmarineDamaged -=
            HandleSubmarineDamaged;

        transform.localPosition =
            transform.localPosition - previousOffset;

        previousOffset =
            Vector3.zero;
    }

    private void LateUpdate()
    {
        if (remainingTime <= 0f)
        {
            transform.localPosition =
                transform.localPosition - previousOffset;

            previousOffset =
                Vector3.zero;

            return;
        }

        remainingTime -=
            Time.deltaTime;

        float progress =
            currentDuration > 0f
                ? 1f - Mathf.Clamp01(
                    remainingTime / currentDuration
                )
                : 1f;

        float fade =
            1f - progress;

        float time =
            Time.time * frequency;

        Vector2 offset =
            new Vector2(
                Mathf.PerlinNoise(seed, time) - 0.5f,
                Mathf.PerlinNoise(seed + 20f, time) - 0.5f
            ) * (currentAmplitude * fade * 2f);

        Vector3 nextOffset =
            new Vector3(
                offset.x,
                offset.y,
                0f
            );

        transform.localPosition =
            transform.localPosition -
            previousOffset +
            nextOffset;

        previousOffset =
            nextOffset;
    }

    private void HandleSubmarineDamaged(
        float amount)
    {
        float scaledAmplitude =
            amplitude +
            amount * damageScale;

        currentDuration =
            duration;

        remainingTime =
            duration;

        currentAmplitude =
            remainingTime > 0f
                ? Mathf.Max(
                    currentAmplitude,
                    scaledAmplitude
                )
                : scaledAmplitude;
    }
}
