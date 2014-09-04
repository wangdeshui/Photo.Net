namespace Photo.Net.Base.Delegate
{
    public interface IPropertyChangeNotify
    {
        event PropertyChangeHandler PropertyChanging;
        event PropertyChangeHandler PropertyChanged;
    }

    public delegate void PropertyChangeHandler(object sender, PropertyChangeArgs arg);

    public class PropertyChangeArgs
    {
        public string PropertyName { get; private set; }

        public PropertyChangeArgs(string name)
        {
            PropertyName = name;
        }
    }
}
