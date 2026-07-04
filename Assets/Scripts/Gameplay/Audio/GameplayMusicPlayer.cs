using UnityEngine;

/// <summary>
/// Gameplay 场景背景音乐入口。只负责播放 Inspector 中拖入的音乐。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class GameplayMusicPlayer : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private AudioSource musicSource;

    [Header("音乐")]
    [Tooltip("直接拖入 Gameplay 背景音乐。为空时不会播放。")]
    [SerializeField] private AudioClip backgroundMusic;

    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private void Reset()
    {
        musicSource = GetComponent<AudioSource>();
        ApplySettings();
    }

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        ApplySettings();
    }

    private void Start()
    {
        if (playOnStart &&
            musicSource != null &&
            backgroundMusic != null)
        {
            musicSource.Play();
        }
    }

    private void ApplySettings()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.playOnAwake = false;
        musicSource.loop = loop;
        musicSource.volume = volume;
        musicSource.spatialBlend = 0f;
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        ApplySettings();
    }
}
