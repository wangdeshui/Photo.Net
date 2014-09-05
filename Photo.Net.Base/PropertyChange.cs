using System;
using System.Runtime.CompilerServices;

namespace Photo.Net.Base
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

    /// <summary>
    /// A basic implement for IPropertyChangeNotify.
    /// </summary>
    public class PropertyChange : IPropertyChangeNotify
    {

        #region Property change

        [field: NonSerialized]
        public event PropertyChangeHandler PropertyChanging;

        [field: NonSerialized]
        public event PropertyChangeHandler PropertyChanged;

        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangeArgs(propertyName));
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangeArgs(propertyName));
            }
        }

        #endregion
    }
}
