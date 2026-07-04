using UnityEngine;

/// <summary>
/// 单个锚发射器的音效配置。所有音频为空时不会影响锚逻辑。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AnchorAudioFeedback2D : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private AudioSource audioSource;

    [Header("锚音效")]
    [Tooltip("锚点持续转向时按间隔重复播放。")]
    [SerializeField] private AudioClip anchorRotateClip;

    [SerializeField] private AudioClip anchorLaunchClip;
    [SerializeField] private AudioClip anchorHitClip;
    [SerializeField] private AudioClip anchorRetractClip;

    [Header("播放设置")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Min(0.01f)]
    [SerializeField] private float rotateRepeatInterval = 0.12f;

    private float nextRotatePlayTime;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        ConfigureSource();
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureSource();
    }

    public void PlayRotate()
    {
        if (Time.unscaledTime < nextRotatePlayTime)
        {
            return;
        }

        nextRotatePlayTime = Time.unscaledTime + rotateRepeatInterval;
        PlayOneShot(anchorRotateClip);
    }

    public void PlayLaunch()
    {
        PlayOneShot(anchorLaunchClip);
    }

    public void PlayHit()
    {
        PlayOneShot(anchorHitClip);
    }

    public void PlayRetract()
    {
        PlayOneShot(anchorRetractClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void ConfigureSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        rotateRepeatInterval = Mathf.Max(0.01f, rotateRepeatInterval);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureSource();
    }
}
