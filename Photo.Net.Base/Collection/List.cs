namespace Photo.Net.Base.Collection
{
    /// <summary>
    /// A very simple linked-list class, done functional style. Use null for
    /// the tail to indicate the end of a list.
    /// </summary>
    public sealed class List
    {
        public object Head { get; private set; }

        public List Tail { get; private set; }

        public List(object head, List tail)
        {
            this.Head = head;
            this.Tail = tail;
        }
    }
}
