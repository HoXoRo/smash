using System.IO;
using ArrowMaze;
using UnityEditor;
using UnityEngine;

public static class ArrowLevelJsonMinifyTool
{
    private const string LevelFolder = "Assets/ArrowGame/ArrowDataLevel";

    [MenuItem("ArrowGame/Level Data/Minify All Level JSON")]
    private static void MinifyAllLevelJson()
    {
        if (!Directory.Exists(LevelFolder))
        {
            EditorUtility.DisplayDialog("Minify Level JSON", $"目录不存在: {LevelFolder}", "OK");
            return;
        }

        string[] files = Directory.GetFiles(LevelFolder, "Level*.json", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            EditorUtility.DisplayDialog("Minify Level JSON", "未找到关卡 JSON 文件。", "OK");
            return;
        }

        long beforeBytes = 0;
        long afterBytes = 0;
        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i].Replace('\\', '/');
            EditorUtility.DisplayProgressBar("Minify Level JSON", file, (float)i / files.Length);

            long sizeBefore = new FileInfo(file).Length;
            beforeBytes += sizeBefore;

            if (ArrowLevelDataUtility.MinifyJsonFile(file))
            {
                successCount++;
                afterBytes += new FileInfo(file).Length;
            }
            else
            {
                failCount++;
                afterBytes += sizeBefore;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        string message =
            $"成功: {successCount}\n失败: {failCount}\n" +
            $"压缩前: {FormatSize(beforeBytes)}\n" +
            $"压缩后: {FormatSize(afterBytes)}\n" +
            $"节省: {FormatSize(beforeBytes - afterBytes)} ({GetSavedPercent(beforeBytes, afterBytes):F1}%)";

        Debug.Log($"[ArrowLevelJsonMinifyTool] {message.Replace("\n", ", ")}");
        EditorUtility.DisplayDialog("Minify Level JSON", message, "OK");
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / 1024f / 1024f:F2} MB";

        return $"{bytes / 1024f:F1} KB";
    }

    private static float GetSavedPercent(long beforeBytes, long afterBytes)
    {
        if (beforeBytes <= 0)
            return 0f;

        return (beforeBytes - afterBytes) * 100f / beforeBytes;
    }
}
