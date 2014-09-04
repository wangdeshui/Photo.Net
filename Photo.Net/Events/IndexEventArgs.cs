using System;

namespace Photo.Net.Events
{
    /// <summary>
    /// Declares an EventArgs type for an event that needs a single integer, interpreted
    /// as an index, as event information.
    /// </summary>
    public sealed class IndexEventArgs
        : EventArgs
    {
        public int Index { get; private set; }

        public IndexEventArgs(int i)
        {
            this.Index = i;
        }
    }
}
