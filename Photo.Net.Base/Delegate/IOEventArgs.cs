using System;

namespace Photo.Net.Base.Delegate
{
    public sealed class IOEventArgs
        : EventArgs
    {
        public IOOperationType IoOperationType { get; private set; }

        public long Position { get; private set; }

        public int Count { get; private set; }

        public IOEventArgs(IOOperationType ioOperationType, long position, int count)
        {
            this.IoOperationType = ioOperationType;
            this.Position = position;
            this.Count = count;
        }
    }


    public enum IOOperationType
    {
        Read,
        Write
    }
}
