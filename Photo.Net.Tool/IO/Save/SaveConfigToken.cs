using System;
using System.Runtime.Serialization;

namespace Photo.Net.Tool.IO.Save
{
    /// <summary>
    /// Config some infomation of save image.
    /// </summary>
    [Serializable]
    public class SaveConfig
        : ICloneable,
          IDeserializationCallback
    {

        public SaveConfig()
        {
        }

        protected SaveConfig(SaveConfig copyMe)
        {
        }

        public virtual void Validate()
        {
        }

        public void OnDeserialization(object sender)
        {
            Validate();
        }

        public virtual object Clone()
        {
            return new SaveConfig(this);
        }
    }
}

