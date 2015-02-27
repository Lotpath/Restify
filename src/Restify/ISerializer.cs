namespace Restify
{
    public interface ISerializer
    {
        string Serialize(object value);
        T Deserialize<T>(string json);
    }
}