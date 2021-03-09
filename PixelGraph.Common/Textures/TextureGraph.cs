﻿using Microsoft.Extensions.DependencyInjection;
using PixelGraph.Common.Encoding;
using PixelGraph.Common.ImageProcessors;
using PixelGraph.Common.ResourcePack;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelGraph.Common.Textures
{
    public interface ITextureGraph
    {
        Task PreBuildNormalTextureAsync(CancellationToken token = default);
        void FixEdges(Image image, string tag, Rectangle? bounds = null);
        int GetMaxFrameCount();

        Task MapAsync(string textureTag, bool createEmpty, int? frame = null, CancellationToken token = default);
        Task<Image<TPixel>> CreateImageAsync<TPixel>(string textureTag, bool createEmpty, CancellationToken token = default) where TPixel : unmanaged, IPixel<TPixel>;
    }

    internal class TextureGraph : ITextureGraph
    {
        private readonly IServiceProvider provider;
        private readonly ITextureGraphContext context;
        private readonly ITextureNormalGraph normalGraph;
        private readonly Dictionary<string, ITextureBuilder> builderMap;
        private bool hasPreBuiltNormals;


        public TextureGraph(IServiceProvider provider)
        {
            this.provider = provider;

            context = provider.GetRequiredService<ITextureGraphContext>();
            normalGraph = provider.GetRequiredService<ITextureNormalGraph>();

            builderMap = new Dictionary<string, ITextureBuilder>(StringComparer.OrdinalIgnoreCase);
            hasPreBuiltNormals = false;
        }

        private bool HasOutputNormals()
        {
            return context.OutputEncoding.Where(e => e.HasMapping)
                .Any(e => {
                    if (EncodingChannel.Is(e.ID, EncodingChannel.NormalX)) return true;
                    if (EncodingChannel.Is(e.ID, EncodingChannel.NormalY)) return true;
                    if (EncodingChannel.Is(e.ID, EncodingChannel.NormalZ)) return true;
                    return false;
                });
        }

        public async Task PreBuildNormalTextureAsync(CancellationToken token = default)
        {
            if (hasPreBuiltNormals || !HasOutputNormals()) return;
            hasPreBuiltNormals = true;

            if (await normalGraph.TryBuildNormalMapAsync(token)) {
                context.InputEncoding.RemoveAll(e => EncodingChannel.Is(e.ID, EncodingChannel.NormalX));
                context.InputEncoding.RemoveAll(e => EncodingChannel.Is(e.ID, EncodingChannel.NormalY));
                context.InputEncoding.RemoveAll(e => EncodingChannel.Is(e.ID, EncodingChannel.NormalZ));

                context.InputEncoding.Add(new ResourcePackNormalXChannelProperties {
                    Texture = TextureTags.NormalGenerated,
                    Color = ColorChannel.Red,
                });

                context.InputEncoding.Add(new ResourcePackNormalYChannelProperties {
                    Texture = TextureTags.NormalGenerated,
                    Color = ColorChannel.Green,
                });

                context.InputEncoding.Add(new ResourcePackNormalZChannelProperties {
                    Texture = TextureTags.NormalGenerated,
                    Color = ColorChannel.Blue,
                });
            }
        }

        public async Task MapAsync(string textureTag, bool createEmpty, int? frame = null, CancellationToken token = default)
        {
            var builder = provider.GetRequiredService<ITextureBuilder>();

            builder.TargetFrame = frame;
            builder.InputChannels = context.InputEncoding.ToArray();
            builder.OutputChannels = context.OutputEncoding
                .Where(e => TextureTags.Is(e.Texture, textureTag)).ToArray();

            await builder.MapAsync(createEmpty, token);
            builderMap[textureTag] = builder;
        }

        public async Task<Image<TPixel>> CreateImageAsync<TPixel>(string textureTag, bool createEmpty, CancellationToken token = default)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            if (!builderMap.TryGetValue(textureTag, out var builder))
                throw new ApplicationException($"No texture builder found for tag '{textureTag}'!");

            return await builder.BuildAsync<TPixel>(createEmpty, token);
        }

        public int GetMaxFrameCount()
        {
            return builderMap.Values.Max(b => b.FrameCount);
        }

        public void FixEdges(Image image, string tag, Rectangle? bounds = null)
        {
            var hasEdgeSizeX = context.Material.Height?.EdgeFadeX.HasValue ?? false;
            var hasEdgeSizeY = context.Material.Height?.EdgeFadeY.HasValue ?? false;
            if (!hasEdgeSizeX && !hasEdgeSizeY) return;

            var heightChannels = context.OutputEncoding
                .Where(c => TextureTags.Is(c.Texture, tag))
                .Where(c => EncodingChannel.Is(c.ID, EncodingChannel.Height))
                .Select(c => c.Color ?? ColorChannel.None).ToArray();

            if (!heightChannels.Any()) return;

            var options = new HeightEdgeProcessor.Options {
                SizeX = (float?)context.Material.Height?.EdgeFadeX ?? 0f,
                SizeY = (float?)context.Material.Height?.EdgeFadeY ?? 0f,
                Colors = heightChannels,
            };

            var processor = new HeightEdgeProcessor(options);
            image.Mutate(c => {
                if (!bounds.HasValue) c.ApplyProcessor(processor);
                else c.ApplyProcessor(processor, bounds.Value);
            });
        }
    }
}
