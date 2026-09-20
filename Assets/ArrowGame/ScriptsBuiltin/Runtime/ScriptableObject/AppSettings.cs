using GameFramework.Resource;
using UnityEngine;

[CreateAssetMenu(fileName = "AppSettings", menuName = "ScriptableObject/AppSettings【App内置配置参数】")]
public class AppSettings : ScriptableObject
{
    private static AppSettings mInstance = null;
    public static AppSettings Instance
    {
        get
        {
            if (mInstance == null)
            {
                mInstance = Resources.Load<AppSettings>("AppSettings");
            }
            return mInstance;
        }
    }
    [Tooltip("debug模式,默认显示debug窗口")]
    public bool DebugMode = false;
    [Tooltip("资源模式: 单机/全热更/需要时热更")]
    public ResourceMode ResourceMode = ResourceMode.Package;
    [Tooltip("屏幕设计分辨率:")]
    public Vector2Int DesignResolution = new Vector2Int(1080, 1920);

    [Header("远程配表")]
    [Tooltip("是否启用远程配表同步")]
    public bool EnableRemoteTableSync = true;
    [Tooltip("远程配表 CDN 根地址，运行时自动追加 Application.version 子目录，例如 https://cdn.example.com/arrow/config/ -> .../config/1.0.0/")]
    public string RemoteTableBaseUrl = string.Empty;
    [Tooltip("远程配表同步超时（秒）")]
    public int RemoteTableSyncTimeoutSeconds = 10;
    [Tooltip("远程配表 JSON 是否加密")]
    public bool EnableRemoteTableEncryption = true;
    [Tooltip("远程配表加密密钥，留空则使用内置默认密钥")]
    public string RemoteTableEncryptKey = string.Empty;
}
