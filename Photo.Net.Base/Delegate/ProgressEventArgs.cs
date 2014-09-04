namespace Photo.Net.Base.Delegate
{
    public class ProgressEventArgs
        : System.EventArgs
    {
        public double Percent { get; private set; }

        public ProgressEventArgs(double percent)
        {
            this.Percent = percent;
        }
    }
}
