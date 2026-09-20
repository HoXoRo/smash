

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityGameFramework.Runtime;


public static class JsonEncryptUtil
{
    private const int MsgFirstLength = 1;
    private static char[] RandomKeys = "0123456789qwertyuiofselzxcvbnmsadas".ToCharArray();

    private const int LZ4_SKIP_SIZE = 128;
    private const int LZ4_CompressionFlagOffset = 1;
    private const int LZ4_LengthFlagOffset = 4;
    public const byte LZ4_UnCompressionFlag = 0x00;
    public const byte LZ4_BeenCompressedFlag = 0x01;
    private readonly static byte[] LZ4_UnCompressionFlagBytes = { LZ4_UnCompressionFlag };
    private readonly static byte[] LZ4_BeenCompressedFlagBytes = { LZ4_BeenCompressedFlag };

    public static void WriteObjectInLocalFile(string path, object obj, string key)
    {
        string text = JsonConvert.SerializeObject(obj);
        WriteInLocalFile(path, text, key);
    }

    public static void WriteInLocalFile(string path, string text, string key)
    {
        // text = Channel.Current.encryptedString(path, text, key);
        File.WriteAllText(path, text);
    }

    public static T ReadFormText<T>(string path, string text, string key) where T : class
    {
        // text = Channel.Current.decryptedString(path, text, key);
        if (string.IsNullOrEmpty(text))
        {
            Log.Error("[JsonEncryptUtil]ReadFormText decryptedString Fail: " + path);
            return null;
        }
        return JsonConvert.DeserializeObject<T>(text);
    }

    public static T ReadFormLocalFile<T>(string path, string key) where T : class
    {
        string text = File.ReadAllText(path);
        // text = Channel.Current.decryptedString(path, text, key);
        if (string.IsNullOrEmpty(text))
        {
            Log.Error("[JsonEncryptUtil]ReadFormLocalFile decryptedString Fail: " + path);
            return null;
        }
        return JsonConvert.DeserializeObject<T>(text);
    }
}
