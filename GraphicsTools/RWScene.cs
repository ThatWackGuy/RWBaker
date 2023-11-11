using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using RWBaker.GeneralTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.SPIRV;

namespace RWBaker.GraphicsTools;

public class RWScene : IDisposable
{
    public uint Width { get; private set; }
    public uint Height { get; private set; }
    
    private readonly GraphicsDevice graphicsDevice;

    private RgbaFloat RenderBg;
    
    private Texture renderTarget;
    private Texture renderTransferTexture;
    private Texture renderObjectStencil;
    
    private Texture renderShadowTarget;
    public Texture RenderShadowSampled;

    private readonly ResourceSet renderShadowDataSet;
    
    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;
    
    private readonly DeviceBuffer shadowDataBuffer;
    
    public Framebuffer LitFramebuffer { get; private set; }
    private Framebuffer shadowFramebuffer;
    private Pipeline shadowPipeline;
    private readonly CommandList commandList;

    private readonly List<RWRenderDescription> renderItems = new();
    
    public Vector2 ObjectOffset = Vector2.Zero;
    public Vector2 LightOffset = Vector2.Zero;

    public Matrix4x4 Transform { get; private set; }

    public RWScene()
    {
        graphicsDevice = Graphics.GraphicsDevice;

        ResourceFactory factory = Graphics.ResourceFactory;

        RenderBg = RgbaFloat.Clear;

        // Draw Buffers
        vertexBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.VertexBuffer)); // 32 * const
        indexBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.IndexBuffer)); // 4 * const
        
        shadowDataBuffer = factory.CreateBuffer(new BufferDescription(96 /*size of shadow uniform*/, BufferUsage.UniformBuffer));
        
        renderShadowDataSet = factory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectDataLayout,
                shadowDataBuffer
            )
        );
        
        Resize(1, 1);

        commandList = factory.CreateCommandList();
    }
    
    public void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;
        
        ResourceFactory factory = graphicsDevice.ResourceFactory;
        
        RecreateTextures(factory);
        RecreatePipelines(factory);
    }
    
    public void ResizeTo(IRWRenderable renderable)
    {
        Vector2Int size = renderable.GetRenderSize(this);
        
        Width = (uint)size.X;
        Height = (uint)size.Y;
        
        ResourceFactory factory = graphicsDevice.ResourceFactory;
        
        RecreateTextures(factory);
        RecreatePipelines(factory);
    }

    private void RecreateTextures(ResourceFactory factory)
    {
        #region Object Textures

        renderTarget = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget,
                TextureType.Texture2D
            )
        );
        renderTarget.Name = "RWScene Render Texture";

        renderObjectStencil = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.D24_UNorm_S8_UInt,
                TextureUsage.DepthStencil,
                TextureType.Texture2D
            )
        );
        renderObjectStencil.Name = "RWScene Object Depth Texture";
        
        renderTransferTexture = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Staging,
                TextureType.Texture2D
            )
        );
        renderTransferTexture.Name = "RWScene Object Transfer Texture";

        #endregion

        #region Shadow Textures
        
        renderShadowTarget = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget,
                TextureType.Texture2D
            )
        );

        RenderShadowSampled = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );

        #endregion
    }

    private void RecreatePipelines(ResourceFactory factory)
    {
        LitFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, renderTarget));
        LitFramebuffer.Name = "LIT FRAMEBUFFER";
        
        shadowFramebuffer  = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, renderShadowTarget));
        shadowFramebuffer.Name = "SHADOW FRAMEBUFFER";
        
        ResourceLayout[] layouts = { RWUtils.RWObjectDataLayout, RWUtils.RWObjectTextureLayout };
        shadowPipeline = factory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                RWUtils.RWShadowShaderSet,
                layouts,
                shadowFramebuffer.OutputDescription
            )
        );
        shadowPipeline.Name = "SHADOW FRAMEBUFFER";
        
        Transform = Matrix4x4.CreateOrthographicOffCenter(
            0,
            Width,
            Height,
            0,
            0,
            -31
        );
    }
    
    public void AddObject(IRWRenderable renderable)
    {
        RWRenderDescription info = renderable.GetSceneInfo(this);
        renderItems.Add(info);
    }

    public void ClearScene()
    {
        renderItems.Clear();
    }

    public void SetBackground(RgbaFloat color)
    {
        RenderBg = color;
    }
    
    public void SetBackground(int type, bool rainBg = false)
    {
        RenderBg = type switch
        {
            0 => RgbaFloat.Clear,
            1 => RgbaFloat.White,
            2 => Palette.Current.ColorsImage[0, rainBg ? 8 : 0].ToRgbaFloat(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private void MapShadows(bool forceUnlit)
    {
        commandList.SetFramebuffer(shadowFramebuffer);
        commandList.ClearColorTarget(0, forceUnlit ? RgbaFloat.Black : RgbaFloat.Clear);
        commandList.ClearDepthStencil(forceUnlit ? 0 : 1, 0);
        
        foreach (RWRenderDescription desc in renderItems)
        {
            commandList.UpdateBuffer(shadowDataBuffer, 0, desc.ShadowData);
            
            commandList.UpdateBuffer(vertexBuffer, 0, desc.Vertices);
            commandList.UpdateBuffer(indexBuffer, 0, desc.Indices);

            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            
            commandList.SetPipeline(shadowPipeline);
            commandList.SetGraphicsResourceSet(0, renderShadowDataSet);
            if (desc.HasTextureSet) commandList.SetGraphicsResourceSet(1, desc.TextureSet);
            
            commandList.DrawIndexed((uint)desc.Indices.Length, 1, 0, 0, 0);
        }
        
        commandList.CopyTexture(renderShadowTarget, RenderShadowSampled);
    }

    private void RenderObjects()
    {
        commandList.SetFramebuffer(LitFramebuffer);
        commandList.ClearColorTarget(0, RenderBg);
        commandList.ClearDepthStencil(1, 0);
        
        foreach (RWRenderDescription desc in renderItems)
        {
            commandList.UpdateBuffer(vertexBuffer, 0, desc.Vertices);
            commandList.UpdateBuffer(indexBuffer, 0, desc.Indices);

            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            
            commandList.SetPipeline(desc.Pipeline);
            commandList.SetGraphicsResourceSet(0, desc.ObjectData); // check RWUniforms
            if (desc.HasTextureSet) commandList.SetGraphicsResourceSet(1, desc.TextureSet);

            commandList.DrawIndexed((uint)desc.Indices.Length, 1, 0, 0, 0);
        }
        
        commandList.CopyTexture(renderTarget, renderTransferTexture);
    }
    
    // Does a completely detached render
    private void TestRender()
    {
        ShaderDescription testV = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.testVert.glsl"), "main", true);
        ShaderDescription testF = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.testFrag.glsl"), "main", true);
        Shader[] shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(testV, testF);
        
        Pipeline pipeline = Graphics.ResourceFactory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Wireframe, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(RWUtils.RWVertexLayout, shaders),
                Array.Empty<ResourceLayout>(),
                LitFramebuffer.OutputDescription
            )
        );
        pipeline.Name = "TEST PIPELINE";
        
        DeviceBuffer VB = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(
                144, // 4 * 36
                BufferUsage.VertexBuffer
            )
        );
        VB.Name = "TEST VB";
        graphicsDevice.UpdateBuffer(VB, 0, RWVertexData.TestQuadVertices(0.7f));

        DeviceBuffer IB = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(
                12, // 6 * 2
                BufferUsage.IndexBuffer
            )
        );
        IB.Name = "TEST IB";
        graphicsDevice.UpdateBuffer(IB, 0, RWVertexData.TestQuadIndices());
        
        commandList.SetFramebuffer(LitFramebuffer);
        commandList.ClearColorTarget(0, RgbaFloat.Clear);
        commandList.ClearDepthStencil(1, 0);
        
        commandList.SetPipeline(pipeline);
        
        commandList.SetVertexBuffer(0, VB);
        commandList.SetIndexBuffer(IB, IndexFormat.UInt16);

        commandList.DrawIndexed(6, 1, 0, 0, 0);
        
        commandList.CopyTexture(renderTarget, renderTransferTexture);
    }
    
    public void Render(bool forceUnlit)
    {
        vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(
                (uint)renderItems.Select(r => r.Vertices.Length).Sum() * 36,
                BufferUsage.VertexBuffer
            )
        );
        
        indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(
                (uint)renderItems.Select(r => r.Indices.Length).Sum() * 4,
                BufferUsage.IndexBuffer
            )
        );

        commandList.Begin();
        
        MapShadows(forceUnlit);
        RenderObjects();

        commandList.End();
    
        // Submit and wait for completion
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();

        ClearScene();
    }

    public MappedResource GetTexData()
    {
        // Read rendered texture
        MappedResource finalRender = graphicsDevice.Map(renderTransferTexture, MapMode.Read);
        return finalRender;
    }
    
    public Texture GetTex()
    {
        Texture tex = Graphics.ResourceFactory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        
        // Read rendered texture
        MappedResource resource = graphicsDevice.Map(renderTransferTexture, MapMode.Read);
        
        // Upload to destination
        graphicsDevice.UpdateTexture(
            tex,
            resource.Data,
            resource.SizeInBytes,
            0,
            0,
            0,
            Width,
            Height,
            1,
            0,
            0
        );

        return tex;
    }

    public void SaveToFile(string path)
    {
        unsafe
        {
            MappedResource renderMap = GetTexData();
            Span<byte> data = new((void*)renderMap.Data, (int)renderMap.SizeInBytes);
            Image<Rgba32> renderedLayer = Image.LoadPixelData<Rgba32>(data, (int)Width, (int)Height);
            FileStream stream = File.Create(path);
            renderedLayer.SaveAsPng(stream);
            stream.Close();
        }
    }

    public void Dispose()
    {
        // TODO: DISPOSE WITHOUT CRASHING EVERYTHING
        
        GC.SuppressFinalize(this);
    }
}