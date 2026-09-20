

public struct KeyValueStruct
{
    public string key;
    public object value;

    public KeyValueStruct(string key, object value)
    {
        this.key = key;
        this.value = value;
    }
}

public class KeyValue
{
    public string key;
    public object value;

    public KeyValue()
    {
    }

    public KeyValue(string key, object value)
    {
        this.key = key;
        this.value = value;
    }
}
