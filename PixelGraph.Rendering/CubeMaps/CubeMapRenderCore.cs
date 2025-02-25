﻿using System;
using System.Threading.Tasks;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PixelGraph.Rendering.CubeMaps
{
    internal abstract class CubeMapRenderCore : RenderCore, ICubeMapSource
    {
        private const float NearField = 0.1f;
        private const float FarField = 100f;

        private readonly Vector3[] targets;
        private readonly Vector3[] lookVector;
        private readonly Vector3[] upVectors;
        private readonly CubeFaceCamerasStruct cubeFaceCameras;
        private readonly RenderTargetView[] renderTargetViews;
        private readonly ConstantBufferComponent modelCB;
        private readonly CommandList[] commands;

        private RasterizerStateProxy invertCullModeState;
        private IDeviceContextPool contextPool;
        protected ShaderPass defaultShaderPass;
        private ShaderResourceViewProxy cubeMap;
        private RasterizerStateProxy rasterState;
        private Viewport viewport;
        private int _faceSize;

        public ShaderResourceViewProxy CubeMap => cubeMap;

        protected string PassName;
        protected Texture2DDescription TextureDesc;
        public long LastUpdated {get; set;}
        
        public int FaceSize {
            get => _faceSize;
            set => SetAffectsRender(ref _faceSize, value);
        }

        protected ShaderPass DefaultShaderPass {
            get => defaultShaderPass;
            private set => SetAffectsRender(ref defaultShaderPass, value);
        }


        protected CubeMapRenderCore(RenderType renderType) : base(renderType)
        {
            lookVector = new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ };
            upVectors = new[] { Vector3.UnitY, Vector3.UnitY, -Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitY, Vector3.UnitY };
            renderTargetViews = new RenderTargetView[6];
            commands = new CommandList[6];
            targets = new Vector3[6];

            cubeFaceCameras = new CubeFaceCamerasStruct {
                Cameras = new CubeFaceCamera[6],
            };

            PassName = DefaultPassNames.Default;
            defaultShaderPass = ShaderPass.NullPass;

            TextureDesc = new Texture2DDescription {
                Format = Format.R8G8B8A8_UNorm,
                ArraySize = 6,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps | ResourceOptionFlags.TextureCube,
                SampleDescription = new SampleDescription(1, 0),
                MipLevels = 0,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
            };

            var modelBufferDesc = new ConstantBufferDescription(DefaultBufferNames.GlobalTransformCB, GlobalTransformStruct.SizeInBytes);
            modelCB = AddComponent(new ConstantBufferComponent(modelBufferDesc));

            UpdateTargets();
        }

        protected abstract void RenderFace(RenderContext context, DeviceContextProxy deviceContext);

        public override void Render(RenderContext context, DeviceContextProxy deviceContext)
        {
            if (CreateCubeMapResources()) {
                RaiseInvalidateRender();
                return;
            }

            context.IsInvertCullMode = true;

            Exception exception = null;
            Parallel.For(0, 6, index => {
                try {
                    var ctx = contextPool.Get();

                    //ctx.ClearRenderTargetView(renderTargetViews[index], Color.Red);

                    ctx.SetRenderTarget(null, renderTargetViews[index]);
                    ctx.SetViewport(ref viewport);
                    ctx.SetScissorRectangle(0, 0, FaceSize, FaceSize);

                    var transforms = new GlobalTransformStruct {
                        Projection = cubeFaceCameras.Cameras[index].Projection,
                        View = cubeFaceCameras.Cameras[index].View,
                        Viewport = new Vector4(FaceSize, FaceSize, 1f / FaceSize, 1f / FaceSize),
                    };

                    transforms.ViewProjection = transforms.View * transforms.Projection;

                    modelCB.Upload(ctx, ref transforms);
                    //Scene.Apply(ctx);

                    DefaultShaderPass.BindShader(ctx);
                    DefaultShaderPass.BindStates(ctx, StateType.BlendState | StateType.DepthStencilState);
                    ctx.SetRasterState(invertCullModeState);

                    //var vertexStartSlot = 0;
                    //if (domeGeometryBuffer.AttachBuffers(ctx, ref vertexStartSlot, EffectTechnique.EffectsManager))
                    //    InstanceBuffer.AttachBuffer(ctx, ref vertexStartSlot);
                    //geometryBuffer.AttachBuffers(ctx, ref vertexStartSlot, EffectTechnique.EffectsManager);

                    //ctx.DrawIndexed(geometryBuffer.IndexBuffer.ElementCount, 0, 0);
                    RenderFace(context, ctx);

                    commands[index] = ctx.FinishCommandList(true);
                    contextPool.Put(ctx);
                }
                catch(Exception ex) {
                    exception = ex;
                }                
            });

            context.IsInvertCullMode = false;

            if (exception != null) throw exception;
            
            for (var i = 0; i < commands.Length; ++i) {
                if (commands[i] == null) continue;

                Device.ImmediateContext.ExecuteCommandList(commands[i], true);
                Disposer.RemoveAndDispose(ref commands[i]);
            }

            LastUpdated = Environment.TickCount64;

            //deviceContext.GenerateMips(cubeMap);
            //context.SharedResource.EnvironmentMapMipLevels = cubeMap.TextureView.Description.TextureCube.MipLevels;

            context.UpdatePerFrameData(true, false, deviceContext);
            //_scene?.Apply(deviceContext);

            //_scene?.ResetValidation();
        }

        protected override bool OnAttach(IRenderTechnique technique)
        {
            DefaultShaderPass = technique[PassName];
            OnDefaultPassChanged(defaultShaderPass);

            contextPool = technique.EffectsManager.DeviceContextPool;

            CreateCubeMapResources();

            //geometryBuffer = Collect(new SkyDomeBufferModel());

            var rasterDesc = new RasterizerStateDescription {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
            };

            CreateRasterState(rasterDesc, true);

            return true;
        }

        protected override void OnDetach()
        {
            contextPool = null;
            cubeMap = null;
            //skyTextureDesc.Width = skyTextureDesc.Height = 0;
            invertCullModeState = null;
            rasterState = null;

            for (var i = 0; i < 6; ++i)
                renderTargetViews[i] = null;

            base.OnDetach();
        }

        //protected void OnElementChanged(object sender, EventArgs e)
        //{
        //    UpdateCanRenderFlag();
        //    RaiseInvalidateRender();
        //}

        protected virtual bool CreateRasterState(RasterizerStateDescription description, bool force)
        {
            var newRasterState = EffectTechnique.EffectsManager.StateManager.Register(description);
            var invCull = description;

            if (description.CullMode != CullMode.None) {
                invCull.CullMode = description.CullMode == CullMode.Back ? CullMode.Front : CullMode.Back;
            }

            var newInvertCullModeState = EffectTechnique.EffectsManager.StateManager.Register(invCull);

            RemoveAndDispose(ref rasterState);
            RemoveAndDispose(ref invertCullModeState);
            rasterState = Collect(newRasterState);
            invertCullModeState = Collect(newInvertCullModeState);
            return true;
        }

        private bool CreateCubeMapResources()
        {
            if (TextureDesc.Width == _faceSize && cubeMap is {IsDisposed: false}) return false;
            
            TextureDesc.Width = TextureDesc.Height = FaceSize;

            RemoveAndDispose(ref cubeMap);
            cubeMap = Collect(new ShaderResourceViewProxy(Device, TextureDesc));

            var srvDesc = new ShaderResourceViewDescription {
                Format = TextureDesc.Format,
                Dimension = ShaderResourceViewDimension.TextureCube,
                TextureCube = new ShaderResourceViewDescription.TextureCubeResource {
                    MostDetailedMip = 0,
                    MipLevels = -1,
                },
            };
            cubeMap.CreateView(srvDesc);

            var rtsDesc = new RenderTargetViewDescription {
                Format = TextureDesc.Format,
                Dimension = RenderTargetViewDimension.Texture2DArray,
                Texture2DArray = new RenderTargetViewDescription.Texture2DArrayResource {
                    MipSlice = 0,
                    FirstArraySlice = 0,
                    ArraySize = 1,
                },
            };

            for (var i = 0; i < 6; ++i) {
                RemoveAndDispose(ref renderTargetViews[i]);
                rtsDesc.Texture2DArray.FirstArraySlice = i;
                renderTargetViews[i] = Collect(new RenderTargetView(Device, cubeMap.Resource, rtsDesc));
            }

            viewport = new Viewport(0, 0, FaceSize, FaceSize);
            return true;
        }

        private void UpdateTargets()
        {
            for (var i = 0; i < 6; ++i) {
                var mView = Matrix.LookAtRH(Vector3.Zero, lookVector[i], upVectors[i]);
                var mProj = Matrix.PerspectiveFovRH((float)Math.PI * 0.5f, 1, NearField, FarField);

                targets[i] = lookVector[i];
                cubeFaceCameras.Cameras[i].View = mView * Matrix.Scaling(-1, 1, 1);
                cubeFaceCameras.Cameras[i].Projection = mProj;
            }
        }

        protected virtual void OnDefaultPassChanged(ShaderPass pass) {}
    }
}
