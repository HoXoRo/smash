using UnityEngine;

public static class RemoteTableCrypto
{
    public static bool IsEnabled()
    {
        AppSettings settings = AppSettings.Instance;
        return settings == null || settings.EnableRemoteTableEncryption;
    }

    public static string EncryptPayload(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        string key = GetEncryptKey();
        return string.IsNullOrEmpty(key)
            ? UtilityBuiltin.DES.Encrypt(plainText)
            : UtilityBuiltin.DES.Encrypt(plainText, key);
    }

    public static string DecryptPayload(string encryptedHex)
    {
        if (string.IsNullOrEmpty(encryptedHex))
        {
            return string.Empty;
        }

        string key = GetEncryptKey();
        return string.IsNullOrEmpty(key)
            ? UtilityBuiltin.DES.Decrypt(encryptedHex)
            : UtilityBuiltin.DES.Decrypt(encryptedHex, key);
    }

    public static string WrapEncryptedJson(string tableName, string encryptedData)
    {
        RemoteTableEncryptedPayload payload = new RemoteTableEncryptedPayload
        {
            Encrypted = true,
            TableName = tableName,
            Data = encryptedData
        };
        return Newtonsoft.Json.JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented);
    }

    private static string GetEncryptKey()
    {
        return AppSettings.Instance?.RemoteTableEncryptKey;
    }
}

public class RemoteTableEncryptedPayload
{
    [Newtonsoft.Json.JsonProperty("encrypted")]
    public bool Encrypted { get; set; }

    [Newtonsoft.Json.JsonProperty("tableName")]
    public string TableName { get; set; }

    [Newtonsoft.Json.JsonProperty("data")]
    public string Data { get; set; }
}
