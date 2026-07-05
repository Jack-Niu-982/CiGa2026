using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 拖到任意 Unity UI Button 上，在按钮 onClick 被触发时播放选择音效。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonSelectionAudio : MonoBehaviour
{
    [Tooltip("默认绑定 Assets/BGMSFX/选择01.wav，可在 Inspector 中替换。")]
    [SerializeField] private AudioClip selectionClip;

    [Range(0f, 2f)]
    [SerializeField] private float volume = 1.1f;

    private Button button;
    private int lastPlayedFrame = -1;

    private void Awake()
    {
        ResolveComponents();
    }

    private void OnEnable()
    {
        ResolveComponents();

        if (button != null)
        {
            button.onClick.RemoveListener(
                PlaySelectionSound
            );

            button.onClick.AddListener(
                PlaySelectionSound
            );
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                PlaySelectionSound
            );
        }
    }

    public void PlaySelectionSound()
    {
        if (selectionClip == null ||
            lastPlayedFrame == Time.frameCount)
        {
            return;
        }

        lastPlayedFrame = Time.frameCount;

        GameObject oneShotObject =
            new GameObject("UI Selection Audio One Shot");

        DontDestroyOnLoad(oneShotObject);

        AudioSource oneShotSource =
            oneShotObject.AddComponent<AudioSource>();

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.ignoreListenerPause = true;
        oneShotSource.PlayOneShot(selectionClip, volume);

        Destroy(
            oneShotObject,
            selectionClip.length + 0.1f
        );
    }

    /// <summary>
    /// 供绕过 Button.onClick 的自定义手柄逻辑主动触发。
    /// </summary>
    public static bool TryPlayFor(Button targetButton)
    {
        if (targetButton == null)
        {
            return false;
        }

        ButtonSelectionAudio feedback =
            targetButton.GetComponent<ButtonSelectionAudio>();

        if (feedback == null)
        {
            return false;
        }

        feedback.PlaySelectionSound();
        return true;
    }

    private void ResolveComponents()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

    }

    private void OnValidate()
    {
        volume = Mathf.Clamp(volume, 0f, 2f);
        ResolveComponents();
    }
}
