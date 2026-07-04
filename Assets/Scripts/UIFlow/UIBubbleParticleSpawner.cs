using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class UIBubbleParticleSpawner : MonoBehaviour
{
    [Header("基础引用")]
    [Tooltip("气泡的 UI Prefab。建议根物体使用 RectTransform，并带有 Image。")]
    [SerializeField] private GameObject bubblePrefab;

    [Tooltip("气泡生成到这个 UI 容器下面。留空则使用当前物体。")]
    [SerializeField] private RectTransform bubbleContainer;

    [Header("渲染层级")]
    [Tooltip("是否自动将气泡容器放到父物体的最底层。")]
    [SerializeField] private bool keepContainerAtBottom = true;

    [Tooltip("新生成的气泡是否放在容器内部的最底层。")]
    [SerializeField] private bool bubbleAsFirstSibling = true;

    [Header("生成设置")]
    [Tooltip("每秒生成多少个气泡。")]
    [Min(0f)]
    [SerializeField] private float bubblesPerSecond = 4f;

    [Tooltip("场上允许同时存在的最大气泡数量。")]
    [Min(1)]
    [SerializeField] private int maxBubbleCount = 50;

    [Tooltip("预先创建多少个气泡，减少运行时卡顿。")]
    [Min(0)]
    [SerializeField] private int initialPoolSize = 15;

    [Tooltip("气泡生成位置距离容器底部的最小高度。")]
    [SerializeField] private float spawnBottomMin = -30f;

    [Tooltip("气泡生成位置距离容器底部的最大高度。")]
    [SerializeField] private float spawnBottomMax = 20f;

    [Tooltip("左右两侧额外扩展的生成范围。")]
    [SerializeField] private float horizontalSpawnPadding = 0f;

    [Header("气泡大小")]
    [Tooltip("气泡随机大小范围，单位为 UI 像素。")]
    [SerializeField] private Vector2 sizeRange = new Vector2(15f, 55f);

    [Header("运动设置")]
    [Tooltip("气泡每秒向上移动的速度范围。")]
    [SerializeField] private Vector2 riseSpeedRange = new Vector2(40f, 100f);

    [Tooltip("气泡水平漂移速度范围。")]
    [SerializeField] private Vector2 horizontalSpeedRange = new Vector2(-15f, 15f);

    [Tooltip("气泡左右摆动的距离范围。")]
    [SerializeField] private Vector2 swayDistanceRange = new Vector2(3f, 20f);

    [Tooltip("气泡左右摆动的速度范围。")]
    [SerializeField] private Vector2 swaySpeedRange = new Vector2(0.5f, 2f);

    [Header("生命周期")]
    [Tooltip("气泡存活时间范围。")]
    [SerializeField] private Vector2 lifetimeRange = new Vector2(3f, 7f);

    [Range(0f, 1f)]
    [Tooltip("气泡生成时的最大透明度。")]
    [SerializeField] private float maximumAlpha = 0.8f;

    [Tooltip("透明度变化曲线。横轴是气泡生命周期，纵轴是透明度倍率。")]
    [SerializeField]
    private AnimationCurve alphaCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 1f),
        new Keyframe(0.7f, 0.8f),
        new Keyframe(1f, 0f)
    );

    [Header("时间设置")]
    [Tooltip("是否使用不受 Time.timeScale 影响的时间。建议 UI 特效开启。")]
    [SerializeField] private bool useUnscaledTime = true;

    private readonly Queue<BubbleParticle> bubblePool = new Queue<BubbleParticle>();
    private readonly List<BubbleParticle> activeBubbles = new List<BubbleParticle>();

    private float spawnTimer;

    private class BubbleParticle
    {
        public GameObject GameObject;
        public RectTransform RectTransform;
        public CanvasGroup CanvasGroup;

        public Vector2 StartPosition;

        public float Age;
        public float Lifetime;

        public float RiseSpeed;
        public float HorizontalSpeed;

        public float SwayDistance;
        public float SwaySpeed;
        public float SwayOffset;
    }

    private void Awake()
    {
        if (bubbleContainer == null)
        {
            bubbleContainer = transform as RectTransform;
        }

        if (keepContainerAtBottom && bubbleContainer != null)
        {
            bubbleContainer.SetAsFirstSibling();
        }

        CreateInitialPool();
    }

    private void OnEnable()
    {
        spawnTimer = 0f;
    }

    private void Update()
    {
        if (bubblePrefab == null || bubbleContainer == null)
        {
            return;
        }

        if (keepContainerAtBottom && bubbleContainer.GetSiblingIndex() != 0)
        {
            bubbleContainer.SetAsFirstSibling();
        }

        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        UpdateBubbleSpawning(deltaTime);
        UpdateActiveBubbles(deltaTime);
    }

    private void CreateInitialPool()
    {
        if (bubblePrefab == null || bubbleContainer == null)
        {
            return;
        }

        int createCount = Mathf.Min(initialPoolSize, maxBubbleCount);

        for (int i = 0; i < createCount; i++)
        {
            BubbleParticle bubble = CreateBubble();
            ReturnBubbleToPool(bubble);
        }
    }

    private BubbleParticle CreateBubble()
    {
        GameObject bubbleObject = Instantiate(
            bubblePrefab,
            bubbleContainer,
            false
        );

        bubbleObject.name = bubblePrefab.name + "_PooledBubble";

        RectTransform bubbleRect =
            bubbleObject.GetComponent<RectTransform>();

        if (bubbleRect == null)
        {
            bubbleRect = bubbleObject.AddComponent<RectTransform>();
        }

        CanvasGroup canvasGroup =
            bubbleObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = bubbleObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        BubbleParticle bubble = new BubbleParticle
        {
            GameObject = bubbleObject,
            RectTransform = bubbleRect,
            CanvasGroup = canvasGroup
        };

        bubbleObject.SetActive(false);

        return bubble;
    }

    private void UpdateBubbleSpawning(float deltaTime)
    {
        if (bubblesPerSecond <= 0f)
        {
            return;
        }

        float spawnInterval = 1f / bubblesPerSecond;

        spawnTimer += deltaTime;

        while (spawnTimer >= spawnInterval)
        {
            spawnTimer -= spawnInterval;

            if (activeBubbles.Count >= maxBubbleCount)
            {
                break;
            }

            SpawnBubble();
        }
    }

    private void SpawnBubble()
    {
        BubbleParticle bubble = GetBubbleFromPool();

        if (bubble == null)
        {
            return;
        }

        Rect containerRect = bubbleContainer.rect;

        float minimumX =
            containerRect.xMin - horizontalSpawnPadding;

        float maximumX =
            containerRect.xMax + horizontalSpawnPadding;

        float spawnX = Random.Range(minimumX, maximumX);

        float spawnY = containerRect.yMin +
                       Random.Range(spawnBottomMin, spawnBottomMax);

        float randomSize = Random.Range(
            Mathf.Min(sizeRange.x, sizeRange.y),
            Mathf.Max(sizeRange.x, sizeRange.y)
        );

        bubble.StartPosition = new Vector2(spawnX, spawnY);

        bubble.RectTransform.anchoredPosition = bubble.StartPosition;
        bubble.RectTransform.sizeDelta =
            new Vector2(randomSize, randomSize);

        bubble.RectTransform.localScale = Vector3.one;
        bubble.RectTransform.localRotation = Quaternion.identity;

        bubble.Age = 0f;

        bubble.Lifetime = Random.Range(
            Mathf.Min(lifetimeRange.x, lifetimeRange.y),
            Mathf.Max(lifetimeRange.x, lifetimeRange.y)
        );

        bubble.Lifetime = Mathf.Max(0.01f, bubble.Lifetime);

        bubble.RiseSpeed = Random.Range(
            Mathf.Min(riseSpeedRange.x, riseSpeedRange.y),
            Mathf.Max(riseSpeedRange.x, riseSpeedRange.y)
        );

        bubble.HorizontalSpeed = Random.Range(
            Mathf.Min(horizontalSpeedRange.x, horizontalSpeedRange.y),
            Mathf.Max(horizontalSpeedRange.x, horizontalSpeedRange.y)
        );

        bubble.SwayDistance = Random.Range(
            Mathf.Min(swayDistanceRange.x, swayDistanceRange.y),
            Mathf.Max(swayDistanceRange.x, swayDistanceRange.y)
        );

        bubble.SwaySpeed = Random.Range(
            Mathf.Min(swaySpeedRange.x, swaySpeedRange.y),
            Mathf.Max(swaySpeedRange.x, swaySpeedRange.y)
        );

        bubble.SwayOffset = Random.Range(0f, Mathf.PI * 2f);

        bubble.CanvasGroup.alpha = 0f;
        bubble.GameObject.SetActive(true);

        if (bubbleAsFirstSibling)
        {
            bubble.RectTransform.SetAsFirstSibling();
        }
        else
        {
            bubble.RectTransform.SetAsLastSibling();
        }

        activeBubbles.Add(bubble);
    }

    private void UpdateActiveBubbles(float deltaTime)
    {
        for (int i = activeBubbles.Count - 1; i >= 0; i--)
        {
            BubbleParticle bubble = activeBubbles[i];

            bubble.Age += deltaTime;

            float normalizedAge = Mathf.Clamp01(
                bubble.Age / bubble.Lifetime
            );

            float verticalMovement =
                bubble.RiseSpeed * bubble.Age;

            float horizontalMovement =
                bubble.HorizontalSpeed * bubble.Age;

            float swayMovement =
                Mathf.Sin(
                    bubble.Age * bubble.SwaySpeed +
                    bubble.SwayOffset
                ) * bubble.SwayDistance;

            bubble.RectTransform.anchoredPosition =
                bubble.StartPosition +
                new Vector2(
                    horizontalMovement + swayMovement,
                    verticalMovement
                );

            bubble.CanvasGroup.alpha =
                alphaCurve.Evaluate(normalizedAge) *
                maximumAlpha;

            if (bubble.Age >= bubble.Lifetime)
            {
                activeBubbles.RemoveAt(i);
                ReturnBubbleToPool(bubble);
            }
        }
    }

    private BubbleParticle GetBubbleFromPool()
    {
        if (bubblePool.Count > 0)
        {
            return bubblePool.Dequeue();
        }

        int totalBubbleCount =
            bubblePool.Count + activeBubbles.Count;

        if (totalBubbleCount >= maxBubbleCount)
        {
            return null;
        }

        return CreateBubble();
    }

    private void ReturnBubbleToPool(BubbleParticle bubble)
    {
        if (bubble == null || bubble.GameObject == null)
        {
            return;
        }

        bubble.CanvasGroup.alpha = 0f;
        bubble.GameObject.SetActive(false);

        bubblePool.Enqueue(bubble);
    }

    private void OnDisable()
    {
        for (int i = activeBubbles.Count - 1; i >= 0; i--)
        {
            ReturnBubbleToPool(activeBubbles[i]);
        }

        activeBubbles.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        bubblesPerSecond = Mathf.Max(0f, bubblesPerSecond);
        maxBubbleCount = Mathf.Max(1, maxBubbleCount);
        initialPoolSize = Mathf.Clamp(
            initialPoolSize,
            0,
            maxBubbleCount
        );

        sizeRange.x = Mathf.Max(0f, sizeRange.x);
        sizeRange.y = Mathf.Max(0f, sizeRange.y);

        lifetimeRange.x = Mathf.Max(0.01f, lifetimeRange.x);
        lifetimeRange.y = Mathf.Max(0.01f, lifetimeRange.y);

        maximumAlpha = Mathf.Clamp01(maximumAlpha);
    }
#endif
}
