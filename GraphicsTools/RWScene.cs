using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RWBaker.GeneralTools;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.SPIRV;

namespace RWBaker.GraphicsTools;

public class RWScene : IDisposable
{
    public uint Width { get; private set; }
    public uint Height { get; private set; }

    private readonly GraphicsDevice graphicsDevice;

    private RgbaFloat RenderBg;

    public GuiTexture ObjectRender;
    private Texture renderObjectStencil;

    public GuiTexture ShadowRender;

    private readonly DeviceBuffer shadowDataBuffer;
    private readonly ResourceSet shadowDataSet;

    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;
    
    public Framebuffer ObjectFramebuffer { get; private set; }
    private Framebuffer shadowFramebuffer;
    private Pipeline shadowPipeline;
    private readonly CommandList commandList;

    private readonly List<RWRenderDescription> renderItems = new();

    public Vector2 ObjectOffset = Vector2.Zero;
    public Vector2 LightOffset = Vector2.Zero;

    public Matrix4x4 Transform { get; private set; }
    public int ShadowRepeat;

    public RWScene()
    {
        graphicsDevice = GuiManager.GraphicsDevice;

        ResourceFactory factory = GuiManager.ResourceFactory;

        RenderBg = RgbaFloat.Clear;

        // Draw Buffers
        vertexBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.VertexBuffer)); // 32 * const
        indexBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.IndexBuffer)); // 4 * const
        
        shadowDataBuffer = factory.CreateBuffer(new BufferDescription(112 /*size of shadow uniform*/, BufferUsage.UniformBuffer));

        shadowDataSet = factory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectDataLayout,
                shadowDataBuffer
            )
        );

        Width = 1;
        Height = 1;
        
        RecreateTextures(factory);
        RecreatePipelines(factory);

        commandList = factory.CreateCommandList();
        ShadowRepeat = 1;
    }

    public void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;

        ResourceFactory factory = graphicsDevice.ResourceFactory;

        RecreateTextures(factory);
        RecreatePipelines(factory);
    }

    public void Resize(IRWRenderable renderable)
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
        ObjectRender?.Dispose();
        Texture objectTarget = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        ObjectRender = GuiTexture.Create("RWScene Object Texture", objectTarget);

        renderObjectStencil?.Dispose();
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

        ShadowRender?.Dispose();
        Texture shadowTarget = factory.CreateTexture(
            new TextureDescription(
                Width,
                Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        ShadowRender = GuiTexture.Create("RWScene Shadow Texture", shadowTarget);
    }

    private void RecreatePipelines(ResourceFactory factory)
    {
        ObjectFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, ObjectRender.Texture));
        ObjectFramebuffer.Name = "Object Framebuffer";

        shadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, ShadowRender.Texture));
        shadowFramebuffer.Name = "Shadow Framebuffer";

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
        shadowPipeline.Name = "Shadow Pipeline";

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
        foreach (RWRenderDescription desc in renderItems)
        {
            desc.Dispose();
        }
        
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
            2 => Program.CurrentPalette.Image[0, rainBg ? 8 : 0].ToRgbaFloat(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private void MapShadows(bool forceUnlit)
    {
        commandList.SetFramebuffer(shadowFramebuffer);
        commandList.ClearColorTarget(0, forceUnlit ? RgbaFloat.Black : RgbaFloat.Red);
        commandList.ClearDepthStencil(forceUnlit ? 0 : 1, 0);

        for (int r = 0; r < renderItems.Count; r++)
        {
            RWRenderDescription desc = renderItems[r];
            
            for (int i = 0; i < ShadowRepeat; i++)
            {
                commandList.UpdateBuffer(shadowDataBuffer, 0, desc.ShadowData);

                commandList.UpdateBuffer(vertexBuffer, 0, desc.Vertices);
                commandList.UpdateBuffer(indexBuffer, 0, desc.Indices);

                commandList.SetVertexBuffer(0, vertexBuffer);
                commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);

                commandList.SetPipeline(shadowPipeline);
                commandList.SetGraphicsResourceSet(0, shadowDataSet);
                if (desc.HasTextureSet) commandList.SetGraphicsResourceSet(1, desc.TextureSet);

                commandList.DrawIndexed((uint)desc.Indices.Length, 1, 0, 0, 0);

                desc.ShadowData.RepeatCurrent++;
            }
        }
    }

    private void RenderObjects()
    {
        commandList.SetFramebuffer(ObjectFramebuffer);
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
    }

    // Does a completely detached render
    private void TestRender()
    {
        ShaderDescription testV = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.test.vert"), "main", true);
        ShaderDescription testF = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.test.frag"), "main", true);
        Shader[] shaders = graphicsDevice.ResourceFactory.CreateFromSpirv(testV, testF);

        Pipeline pipeline = GuiManager.ResourceFactory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Wireframe, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(RWUtils.RWVertexLayout, shaders),
                Array.Empty<ResourceLayout>(),
                ObjectFramebuffer.OutputDescription
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

        commandList.SetFramebuffer(ObjectFramebuffer);
        commandList.ClearColorTarget(0, RgbaFloat.Clear);
        commandList.ClearDepthStencil(1, 0);

        commandList.SetPipeline(pipeline);

        commandList.SetVertexBuffer(0, VB);
        commandList.SetIndexBuffer(IB, IndexFormat.UInt16);

        commandList.DrawIndexed(6, 1, 0, 0, 0);
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

    public void SaveToFile(string path)
    {
        // Copy the color render target into a staging texture
        Texture copyTexture = graphicsDevice.ResourceFactory.CreateTexture(
            new TextureDescription(
                ObjectRender.Texture.Width,
                ObjectRender.Texture.Height,
                ObjectRender.Texture.Depth,
                ObjectRender.Texture.MipLevels,
                ObjectRender.Texture.ArrayLayers,
                ObjectRender.Texture.Format,
                TextureUsage.Staging,
                ObjectRender.Texture.Type
            )
        );
        Fence fence = graphicsDevice.ResourceFactory.CreateFence(false);

        commandList.Begin();
        commandList.CopyTexture(ObjectRender.Texture, copyTexture);
        commandList.End();
        graphicsDevice.SubmitCommands(commandList, fence);
        graphicsDevice.WaitForFence(fence);

        // Save the staging texture as a PNG
        try
        {
            ImageUtils.ToImage(graphicsDevice, copyTexture).Save(path);
        }
        finally
        {
            copyTexture.Dispose();
            fence.Dispose();
        }
    }

    public void Dispose()
    {
        graphicsDevice.WaitForIdle();
        ClearScene();

        shadowDataSet.Dispose();

        ObjectRender.Dispose();
        renderObjectStencil.Dispose();
        ObjectFramebuffer.Dispose();

        ShadowRender.Dispose();
        shadowDataSet.Dispose();
        shadowFramebuffer.Dispose();

        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        
        commandList.Dispose();
        
        GC.SuppressFinalize(this);
    }
}