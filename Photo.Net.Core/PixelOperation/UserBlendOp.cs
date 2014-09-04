﻿using System;

namespace Photo.Net.Core.PixelOperation
{
    /// <summary>
    /// Abstract base class that all "user" blend ops derive from.
    /// These ops are available in the UI for a user to choose from
    /// in order to configure the blending properties of a Layer.
    /// 
    /// See UserBlendOps.cs for guidelines on implementation.
    /// </summary>
    [Serializable]
    public abstract class UserBlendOp
        : BinaryPixelOp
    {
        public virtual UserBlendOp CreateWithOpacity(int opacity)
        {
            return this;
        }

        public override string ToString()
        {
            return Utility.GetStaticName(this.GetType());
        }
    }
}