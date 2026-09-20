using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class RemoteTableManifest
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("files")]
    public Dictionary<string, RemoteTableManifestEntry> Files { get; set; } = new Dictionary<string, RemoteTableManifestEntry>();
}

[Serializable]
public class RemoteTableManifestEntry
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("md5")]
    public string Md5 { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }
}

public enum RemoteTableKind
{
    Config,
    DataTable
}
