using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainManuButtonController : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("开始游戏后跳转的场景")]
    [Tooltip("留空则使用 SceneSettings.chooseCharacter")]
    [SerializeField] private string targetSceneNameOverride;

    [Header("手柄控制")]
    [Tooltip("是否允许使用手柄按钮控制主菜单")]
    [SerializeField] private bool enableGamepadInput = true;

    private bool isLoadingScene;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void Update()
    {
        if (!enableGamepadInput)
        {
            return;
        }

        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        // South：
        // Xbox A
        // PlayStation ×
        // Nintendo B
        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            StartGame();
            return;
        }

        // East：
        // Xbox B
        // PlayStation ○
        // Nintendo A
        if (gamepad.buttonEast.wasPressedThisFrame)
        {
            QuitGame();
        }
    }

    /// <summary>
    /// 开始游戏并跳转到目标场景。
    /// </summary>
    public void StartGame()
    {
        if (isLoadingScene)
        {
            return;
        }

        string targetSceneName =
            !string.IsNullOrWhiteSpace(targetSceneNameOverride)
                ? targetSceneNameOverride
                : SettingManager.Scene.chooseCharacter;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError(
                "MainManuButtonController：Target Scene Name 没有填写！",
                this
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                $"MainManuButtonController：找不到场景“{targetSceneName}”。" +
                "请确认场景名称正确，并且已经添加到 Build Profiles 的场景列表中。",
                this
            );

            return;
        }

        isLoadingScene = true;

        ButtonSelectionAudio.TryPlayFor(
            startButton
        );

        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 完全退出游戏。
    /// </summary>
    public void QuitGame()
    {
        ButtonSelectionAudio.TryPlayFor(
            quitButton
        );

        Debug.Log("退出游戏。");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }
}
