﻿/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using Photo.Net.Core.PixelOperation;
using Photo.Net.Gdi.Renders;
using Photo.Net.Tool.Core.Enums;

namespace Photo.Net.Tool.Core
{
    [Serializable]
    public sealed class GradientInfo
        : ICloneable
    {
        private GradientType gradientType;
        private bool alphaOnly;

        public GradientType GradientType
        {
            get
            {
                return this.gradientType;
            }
        }

        public bool AlphaOnly
        {
            get
            {
                return this.alphaOnly;
            }
        }

        public GradientRenderer CreateGradientRenderer()
        {
            var normalBlendOp = new UserBlendOps.NormalBlendOp();

            switch (this.gradientType)
            {
                case GradientType.LinearClamped:
                    return new GradientRenderers.LinearClamped(this.alphaOnly, normalBlendOp);

                case GradientType.LinearReflected:
                    return new GradientRenderers.LinearReflected(this.alphaOnly, normalBlendOp);

                case GradientType.LinearDiamond:
                    return new GradientRenderers.LinearDiamond(this.alphaOnly, normalBlendOp);

                case GradientType.Radial:
                    return new GradientRenderers.Radial(this.alphaOnly, normalBlendOp);

                case GradientType.Conical:
                    return new GradientRenderers.Conical(this.alphaOnly, normalBlendOp);

                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public override bool Equals(object obj)
        {
            GradientInfo asGI = obj as GradientInfo;

            if (asGI == null)
            {
                return false;
            }

            return (asGI.GradientType == this.GradientType && asGI.AlphaOnly == this.AlphaOnly);
        }

        public override int GetHashCode()
        {
            return unchecked(this.gradientType.GetHashCode() + this.alphaOnly.GetHashCode());
        }

        public GradientInfo(GradientType gradientType, bool alphaOnly)
        {
            this.gradientType = gradientType;
            this.alphaOnly = alphaOnly;
        }

        public GradientInfo Clone()
        {
            return new GradientInfo(this.gradientType, this.alphaOnly);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

}
