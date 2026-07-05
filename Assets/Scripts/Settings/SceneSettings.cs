using UnityEngine;

[CreateAssetMenu(
    fileName = "SceneSettings",
    menuName = "Settings/Scene Settings")]
public class SceneSettings : ScriptableObject
{
    [Header("Scene Names")]
    [Tooltip("主菜单场景名。")]
    public string mainMenu = "MainMenu Jaeger";

    [Tooltip("角色选择场景名。")]
    public string chooseCharacter = "ChooseCharactor";

    [Tooltip("游戏场景名。")]
    public string gameplay = "Jaeger";
}
