using System;

namespace Photo.Net.Base
{
    public interface ICloneable<T>
        : ICloneable
    {
        new T Clone();
    }
}
