using System;

namespace Photo.Net.Base.Delegate
{
    public class EventArgs<T>
        : EventArgs
    {
        public T Data { get; private set; }

        public EventArgs(T data)
        {
            this.Data = data;
        }
    }
}
