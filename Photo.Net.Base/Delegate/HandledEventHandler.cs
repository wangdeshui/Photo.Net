using System.ComponentModel;

namespace Photo.Net.Base.Delegate
{
    public delegate void HandledEventHandler<T>(object sender, HandledEventArgs<T> e);

    public class HandledEventArgs<T>
        : HandledEventArgs
    {
        private T data;
        public T Data
        {
            get
            {
                return this.data;
            }
        }

        public HandledEventArgs(bool handled, T data)
            : base(handled)
        {
            this.data = data;
        }
    }
}
