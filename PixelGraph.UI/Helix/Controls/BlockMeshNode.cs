﻿using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using PixelGraph.UI.Helix.Models;
using System;

namespace PixelGraph.UI.Helix.Controls
{
    internal class BlockMeshNode : MeshNode
    {
        protected override RenderCore OnCreateRenderCore()
        {
            return new BlockMeshRenderCore();
        }

        protected override IAttachableBufferModel OnCreateBufferModel(Guid modelGuid, Geometry3D geometry)
        {
            if (geometry != null && geometry.IsDynamic)
                throw new NotImplementedException("Dynamic block meshes not currently supported!");

            return EffectsManager.GeometryBufferManager.Register<BlockMeshGeometryBufferModel>(modelGuid, geometry);
        }
    }
}
