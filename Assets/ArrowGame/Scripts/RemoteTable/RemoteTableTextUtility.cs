using System.Text;

public static class RemoteTableTextUtility
{
    public static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    public static string BytesToJsonString(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return string.Empty;
        }

        int offset = 0;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            offset = 3;
        }

        return Encoding.UTF8.GetString(bytes, offset, bytes.Length - offset);
    }

    public static string TrimBom(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text[0] == '\uFEFF' ? text.TrimStart('\uFEFF') : text;
    }
}
