using System.IO;
using System.Runtime.Serialization;

namespace Photo.Net.Base.Serializable
{
    public interface IDeferredSerializable
        : ISerializable
    {
        void FinishSerialization(Stream output, DeferredFormatter context);
        void FinishDeserialization(Stream input, DeferredFormatter context);
    }
}
