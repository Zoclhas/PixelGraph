﻿using PixelGraph.Common.Material;
using PixelGraph.Common.ResourcePack;
using PixelGraph.Common.Textures;
using System;
using System.Collections.Generic;

namespace PixelGraph.UI.Internal.Preview.Textures
{
    public interface IRenderNormalsPreviewBuilder : ITexturePreviewBuilder {}

    internal class RenderNormalsPreviewBuilder : TexturePreviewBuilderBase, IRenderNormalsPreviewBuilder
    {
        public RenderNormalsPreviewBuilder(IServiceProvider provider) : base(provider)
        {
            TagMap = tagMap;
        }
        
        private static readonly Dictionary<string, Func<ResourcePackProfileProperties, MaterialProperties, ResourcePackChannelProperties[]>> tagMap =
            new(StringComparer.InvariantCultureIgnoreCase) {
                [TextureTags.Color] = (profile, mat) => new ResourcePackChannelProperties[] {
                    new ResourcePackOpacityChannelProperties(TextureTags.Color, ColorChannel.Alpha) {
                        //Sampler = mat?.Opacity?.Input?.Sampler ?? profile?.Encoding?.Opacity?.Sampler,
                        MaxValue = 255m,
                        DefaultValue = 255m,
                    },
                },
                [TextureTags.Normal] = (profile, mat) => new ResourcePackChannelProperties[] {
                    new ResourcePackNormalXChannelProperties(TextureTags.Normal, ColorChannel.Red) {
                        DefaultValue = 0.5m,
                    },
                    new ResourcePackNormalYChannelProperties(TextureTags.Normal, ColorChannel.Green) {
                        DefaultValue = 0.5m,
                    },
                    new ResourcePackNormalZChannelProperties(TextureTags.Normal, ColorChannel.Blue) {
                        DefaultValue = 1m,
                    },
                    new ResourcePackHeightChannelProperties(TextureTags.Normal, ColorChannel.Alpha) {
                        DefaultValue = 0m,
                        Invert = true,
                    },
                },
            };
    }
}
