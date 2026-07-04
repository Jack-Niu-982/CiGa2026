using UnityEngine;

/// <summary>
/// Gameplay 原型期调试面板。
/// 这里集中放少量测试按钮，不进入正式 UIFlow。
/// </summary>
[DisallowMultipleComponent]
public class GameplayDebugPanel : MonoBehaviour
{
    [Header("血量测试")]
    [SerializeField]
    private float testDamageAmount = 10f;

    [SerializeField]
    private float testRepairAmount = 10f;

    [Header("面板")]
    [SerializeField]
    private bool showPanel = true;

    [SerializeField]
    private Vector2 panelPosition =
        new Vector2(12f, 12f);

    [SerializeField]
    private Vector2 panelSize =
        new Vector2(220f, 124f);

    private GUIStyle titleStyle;

    private void OnGUI()
    {
        if (!showPanel)
        {
            return;
        }

        EnsureStyles();

        Rect areaRect =
            new Rect(
                panelPosition.x,
                panelPosition.y,
                panelSize.x,
                panelSize.y
            );

        GUILayout.BeginArea(
            areaRect,
            GUI.skin.window
        );

        GUILayout.Label(
            "DebugPanel",
            titleStyle
        );

        DrawHealthTestControls();

        GUILayout.EndArea();
    }

    private void DrawHealthTestControls()
    {
        GUILayout.Label(
            "Submarine Health"
        );

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(
                $"扣血测试 -{testDamageAmount:0}",
                GUILayout.Height(30f)))
        {
            GameplayEventBus.RequestSubmarineDamage(
                testDamageAmount
            );
        }

        if (GUILayout.Button(
                $"加血测试 +{testRepairAmount:0}",
                GUILayout.Height(30f)))
        {
            GameplayEventBus.RequestSubmarineRepair(
                testRepairAmount
            );
        }

        GUILayout.EndHorizontal();
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle =
            new GUIStyle(GUI.skin.label)
            {
                fontStyle =
                    FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
    }
}
