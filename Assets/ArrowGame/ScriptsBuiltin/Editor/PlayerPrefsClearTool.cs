using UnityEditor;
using UnityEngine;

public static class PlayerPrefsClearTool
{
    private const string MenuPath = "Game Framework/GameTools/Clear PlayerPrefs【清空本地存档】";

    [MenuItem(MenuPath, false, 1002)]
    private static void ClearPlayerPrefs()
    {
        if (!EditorUtility.DisplayDialog(
                "Clear PlayerPrefs",
                "将清空所有 PlayerPrefs 本地数据，此操作不可恢复。是否继续？",
                "Clear",
                "Cancel"))
        {
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs.DeleteAll() 已执行。");
    }
}
