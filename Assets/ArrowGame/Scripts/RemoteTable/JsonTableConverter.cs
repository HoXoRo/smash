using System;
using System.IO;
using System.Text;
using GameFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonTableConverter
{
    public static byte[] ConvertToBytes(RemoteTableKind kind, string tableName, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Remote table json is empty.", nameof(json));
        }

        string payload = ExtractPayload(json, tableName);
        return kind == RemoteTableKind.Config
            ? ConvertConfigJsonToBytes(payload)
            : ConvertDataTablePayloadToBytes(tableName, payload);
    }

    private static string ExtractPayload(string json, string tableName)
    {
        JObject root = JObject.Parse(json);
        if (!IsEncryptedWrapper(root))
        {
            return json;
        }

        string encryptedData = root["data"]?.ToString();
        if (string.IsNullOrWhiteSpace(encryptedData))
        {
            throw new GameFrameworkException(Utility.Text.Format(
                "Encrypted remote table data is empty. Table='{0}'.", tableName));
        }

        string decrypted = RemoteTableCrypto.DecryptPayload(encryptedData);
        if (string.IsNullOrWhiteSpace(decrypted))
        {
            throw new GameFrameworkException(Utility.Text.Format(
                "Decrypt remote table failed. Table='{0}'.", tableName));
        }

        return decrypted;
    }

    private static bool IsEncryptedWrapper(JObject root)
    {
        if (root["encrypted"]?.Type == JTokenType.Boolean && root["encrypted"].Value<bool>())
        {
            return true;
        }

        return root["data"] != null && root["bytesBase64"] == null;
    }

    private static byte[] ConvertConfigJsonToBytes(string json)
    {
        JObject root = JObject.Parse(json);
        if (root["bytesBase64"] != null)
        {
            return DecodeBase64(root["bytesBase64"]?.ToString(), "Config");
        }

        using MemoryStream memoryStream = new MemoryStream();
        using BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8);
        foreach (JProperty property in root.Properties())
        {
            if (property.Name is "tableName" or "bytesBase64" or "encrypted" or "data")
            {
                continue;
            }

            binaryWriter.Write(property.Name);
            binaryWriter.Write(GetTokenString(property.Value));
        }

        return memoryStream.ToArray();
    }

    private static byte[] ConvertDataTablePayloadToBytes(string tableName, string payload)
    {
        string trimmedPayload = payload.Trim();
        if (!trimmedPayload.StartsWith("{", StringComparison.Ordinal))
        {
            return DecodeBase64(trimmedPayload, tableName);
        }

        JObject root = JObject.Parse(payload);
        string bytesBase64 = root["bytesBase64"]?.ToString();
        if (!string.IsNullOrWhiteSpace(bytesBase64))
        {
            return DecodeBase64(bytesBase64, tableName);
        }

        throw new GameFrameworkException(Utility.Text.Format(
            "Remote data table payload is invalid. Table='{0}'.", tableName));
    }

    private static byte[] DecodeBase64(string base64, string tableName)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new GameFrameworkException(Utility.Text.Format(
                "Remote table base64 is empty. Table='{0}'.", tableName));
        }

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (Exception exception)
        {
            throw new GameFrameworkException(Utility.Text.Format(
                "Decode remote table base64 failed. Table='{0}', Error='{1}'.", tableName, exception.Message));
        }
    }

    private static string GetTokenString(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
        {
            return string.Empty;
        }

        if (token.Type == JTokenType.String)
        {
            return token.Value<string>() ?? string.Empty;
        }

        if (token.Type == JTokenType.Boolean)
        {
            return token.Value<bool>() ? "True" : "False";
        }

        return token.ToString(Formatting.None);
    }
}
