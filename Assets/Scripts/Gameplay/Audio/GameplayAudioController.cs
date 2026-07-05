using UnityEngine;

/// <summary>
/// Gameplay 场景的集中音频入口。
/// 音频资源保留为 Inspector 引用，玩法脚本只负责通知事件，不持有具体 AudioClip。
/// </summary>
[DisallowMultipleComponent]
public class GameplayAudioController : MonoBehaviour
{
    [Header("播放通道")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("循环音频")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip submarineAmbience;

    [Header("玩家事件")]
    [SerializeField] private AudioClip[] operateAnchorClips;
    [SerializeField] private AudioClip[] meowClips;
    [SerializeField] private AudioClip[] materialPickupClips;
    [SerializeField] private AudioClip interactionAvailableClip;

    [Header("飞船事件")]
    [SerializeField] private AudioClip[] submarineCollisionClips;
    [SerializeField] private AudioClip[] depositSuccessClips;

    [Header("船锚事件")]
    [SerializeField] private AudioClip[] longAnchorLaunchClips;
    [SerializeField] private AudioClip anchorCaughtClip;

    [Header("音量")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.65f;

    [Range(0f, 1f)]
    [SerializeField] private float ambienceVolume = 0.5f;

    [Range(0f, 2f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("防止碰撞音效堆叠")]
    [Min(0f)]
    [SerializeField] private float collisionSoundCooldown = 0.12f;

    private static GameplayAudioController instance;

    private MissionSettlementController settlementController;
    private float nextCollisionSoundTime;

    public static GameplayAudioController Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    private void Reset()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length > 0)
        {
            bgmSource = sources[0];
        }

        if (sources.Length > 1)
        {
            ambienceSource = sources[1];
        }

        if (sources.Length > 2)
        {
            sfxSource = sources[2];
        }

        ConfigureSources();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                "场景中存在多个 GameplayAudioController，已停用重复实例。",
                this
            );

            enabled = false;
            return;
        }

        instance = this;
        ConfigureSources();
        ResolveSettlementController();
    }

    private void OnEnable()
    {
        ResolveSettlementController();

        if (settlementController != null)
        {
            settlementController.MissionSettled += HandleMissionSettled;
        }
    }

    private void Start()
    {
        PlayLoop(bgmSource, backgroundMusic, bgmVolume);
        PlayLoop(ambienceSource, submarineAmbience, ambienceVolume);
    }

    private void OnDisable()
    {
        if (settlementController != null)
        {
            settlementController.MissionSettled -= HandleMissionSettled;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void PlayOperateStarted()
    {
        if (instance == null)
        {
            return;
        }

        instance.PlayRandom(instance.operateAnchorClips);
    }

    public static void PlayPlayerPickedUpItem()
    {
        if (instance == null)
        {
            return;
        }

        instance.PlayRandom(instance.materialPickupClips);
        instance.PlayRandom(instance.meowClips);
    }

    public static void PlaySubmarineCollision()
    {
        if (instance == null ||
            Time.unscaledTime < instance.nextCollisionSoundTime)
        {
            return;
        }

        instance.nextCollisionSoundTime =
            Time.unscaledTime + instance.collisionSoundCooldown;

        instance.PlayRandom(instance.submarineCollisionClips);
    }

    public static void PlayDepositSuccess()
    {
        if (instance != null)
        {
            instance.PlayRandom(instance.depositSuccessClips);
        }
    }

    public static void PlayInteractionAvailable()
    {
        if (instance != null)
        {
            instance.PlayOneShot(instance.interactionAvailableClip);
        }
    }

    public static void PlayAnchorLaunched()
    {
        if (instance != null)
        {
            instance.PlayRandom(instance.longAnchorLaunchClips);
        }
    }

    public static void PlayAnchorCaughtItem()
    {
        if (instance != null)
        {
            instance.PlayOneShot(instance.anchorCaughtClip);
        }
    }

    private void HandleMissionSettled(MissionSettlementState state)
    {
        if (state != MissionSettlementState.Running &&
            ambienceSource != null)
        {
            ambienceSource.Stop();
        }
    }

    private void ResolveSettlementController()
    {
        if (settlementController == null)
        {
            settlementController =
                FindObjectOfType<MissionSettlementController>();
        }
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        int startIndex = Random.Range(0, clips.Length);

        for (int offset = 0; offset < clips.Length; offset++)
        {
            AudioClip clip =
                clips[(startIndex + offset) % clips.Length];

            if (clip != null)
            {
                PlayOneShot(clip);
                return;
            }
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    private static void PlayLoop(
        AudioSource source,
        AudioClip clip,
        float volume)
    {
        if (source == null || clip == null)
        {
            return;
        }

        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.Play();
    }

    private void ConfigureSources()
    {
        ConfigureSource(bgmSource, bgmVolume);
        ConfigureSource(ambienceSource, ambienceVolume);
        ConfigureSource(sfxSource, 1f);
    }

    private static void ConfigureSource(
        AudioSource source,
        float volume)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
    }

    private void OnValidate()
    {
        bgmVolume = Mathf.Clamp01(bgmVolume);
        ambienceVolume = Mathf.Clamp01(ambienceVolume);
        sfxVolume = Mathf.Clamp(sfxVolume, 0f, 2f);
        collisionSoundCooldown = Mathf.Max(0f, collisionSoundCooldown);
        ConfigureSources();
    }
}
