﻿using PixelGraph.Common.Samplers;
using System.Collections.Generic;

namespace PixelGraph.UI.ViewData
{
    internal class OcclusionSamplerValues : List<OcclusionSamplerValues.Item>
    {
        public OcclusionSamplerValues()
        {
            Add(new Item {Text = "Nearest", Value = Sampler.Nearest});
            Add(new Item {Text = "Bilinear", Value = Sampler.Bilinear});
        }

        public class Item
        {
            public string Text {get; set;}
            public string Value {get; set;}
        }
    }
}
