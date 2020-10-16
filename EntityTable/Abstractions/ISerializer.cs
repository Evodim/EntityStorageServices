namespace Evod.Toolkit.Core.Abstractions
{
    /// <summary>
    /// Interface to serialize any kind of transport
    /// </summary>
    /// <remarks>Work in progress</remarks>
    /// <typeparam name="T">Transport (sample: string for json , byte for protobuf)</typeparam>

    public interface ISerializer<T>
    {
        T Parse(string content, T obj);

        string Serialize(T obj);
    }

    /// <summary>
    /// Serializer based on a generic transport
    /// exemple: string => json; byte => message pack or bson
    /// </summary>
    public interface IContentSerializer
    {
        T Parse<T>(object sourceObject);

        object Serialize<T>(T content);
    }
}