using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using SixLabors.ImageSharp;
using Veldrid;

namespace RWBaker.Rendering;

public enum CameraBgState
{
    Clear,
    Sky,
    White,
    Custom
}

public class Camera : SceneObject, IInspectable, IDisposable
{
    public float SunAzimuth;
    public float SunAltitude;
    private Vector3 sunPosition;

    public float ShadowBias = 0.0003f;

    public float StaticDepth = 5;

    public uint Width => (uint)Size.X;
    public uint Height => (uint)Size.Y;

    public Vector2 ProjectorOffset;

    public readonly Guid Id;

    public CameraBgState BackgroundState;
    public RgbaFloat BackgroundColor;

    private DeviceBuffer _vtxBuffer;
    private DeviceBuffer _idxBuffer;

    private readonly Dictionary<string, Pipeline> _pipelineCache = new();

    public RenderPass RemovalPass { get; private set; }
    public RenderPass LightingPass { get; private set; }
    public RenderPass ColorPass { get; private set; }

    private readonly DeviceBuffer _camBuffer;
    private readonly DeviceBuffer _lightingBuffer;
    private readonly DeviceBuffer _passBuffer;

    private DeviceBuffer _objectBuffer;

    private ResourceSet _infoResourceSet;

    public Matrix4x4 ProjectionView { get; private set; }
    public Matrix4x4 LightingProjectionView { get; private set; }

    public bool Unlit;
    public float RainPercentage;

    public bool WaitingForRecreate = true;

    public Camera(Scene owner) : base(owner, "Camera")
    {
        Size = Vector2.One;

        SunAzimuth = 0;
        SunAltitude = 0;

        Id = Guid.NewGuid();

        BackgroundState = CameraBgState.Clear;
        BackgroundColor = RgbaFloat.Clear;

        Unlit = false;
        RainPercentage = 0;

        ResourceFactory factory = GuiManager.ResourceFactory;

        _vtxBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.VertexBuffer));
        _idxBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.IndexBuffer));

        RemovalPass = new RenderPass(factory, $"removal{Id}", BlendStateDescription.SingleAlphaBlend, Width, Height);
        LightingPass = new RenderPass(factory, $"lighting{Id}", BlendStateDescription.SingleAlphaBlend, Width, Height);
        ColorPass = new RenderPass(factory, $"color{Id}", BlendStateDescription.SingleAlphaBlend, Width, Height);

        ProjectionView = Matrix4x4.Identity;

        _camBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
        _passBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        _lightingBuffer = factory.CreateBuffer(new BufferDescription(144, BufferUsage.UniformBuffer));

        _objectBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        _infoResourceSet = factory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectDataLayout,
                _camBuffer,
                _passBuffer,
                _lightingBuffer,
                Scene.PaletteManager.PaletteInfo,
                _objectBuffer,
                GuiManager.GraphicsDevice.PointSampler
            )
        );
    }

    public void RenderInspector()
    {
        ImGui.TextDisabled($"id: {Id}");

        if (ImGui.SliderFloat("Play Layer", ref StaticDepth, 0f, 30f)) WaitingForRecreate = true;

        ImGui.SliderFloat("Rain Percentage", ref RainPercentage, 0, 1);

        ImGui.Checkbox("Unlit", ref Unlit);

        string comboPreview = BackgroundState switch
        {
            CameraBgState.Clear => "Clear",
            CameraBgState.Sky => "Sky",
            CameraBgState.White => "White",
            CameraBgState.Custom => "Custom",
            _ => throw new ArgumentOutOfRangeException(nameof(BackgroundState))
        };

        if (ImGui.BeginCombo("##background", comboPreview))
        {
            if (ImGui.Selectable("Clear"))
            {
                SetBackground(CameraBgState.Clear);
            }
            else if (ImGui.Selectable("Sky"))
            {
                SetBackground(CameraBgState.Sky);
            }
            else if (ImGui.Selectable("White"))
            {
                SetBackground(CameraBgState.White);
            }

            ImGui.EndCombo();
        }

        if (BackgroundState == CameraBgState.Custom)
        {
            Vector4 col = BackgroundColor.ToVector4();
            if (ImGui.ColorPicker4("Custom Background", ref col)) BackgroundColor = new RgbaFloat(col);
        }

        ImGui.SeparatorText("SUN SETTINGS");

        if (ImGui.SliderAngle("Sun Azimuth", ref SunAzimuth)) WaitingForRecreate = true;
        if (ImGui.SliderAngle("Sun Altitude", ref SunAltitude)) WaitingForRecreate = true;
        if (ImGui.DragFloat("Shadow Bias", ref ShadowBias, 0.000001f, 0f, 0.0001f)) WaitingForRecreate = true;

        ImGui.Text($"Sun at: {sunPosition.ToString()}");

        ImGui.SeparatorText("PROJECTOR SETTINGS");

        if (ImGui.SliderFloat("##projectorx", ref ProjectorOffset.X, -500, 500)) WaitingForRecreate = true;
        if (ImGui.SliderFloat("##projectory", ref ProjectorOffset.Y, -500, 500)) WaitingForRecreate = true;

        ImGui.Separator();

        if (ImGui.Button("Save Camera As Image"))
        {
            Directory.CreateDirectory("./Cameras/");
            SaveToFile($"./Cameras/Camera_{Id}.png");
        }
    }

    public void SetBackground(RgbaFloat color)
    {
        BackgroundColor = color;
    }

    public void SetBackground(CameraBgState state)
    {
        BackgroundState = state;

        BackgroundColor = state switch
        {
            CameraBgState.Clear => RgbaFloat.Clear,
            CameraBgState.Sky => Scene.PaletteManager.CurrentPalette.Image[0, 0].MixRGB(Scene.PaletteManager.CurrentPalette.Image[0, 8], RainPercentage).ToRgbaFloat(), // Mix rain sky based on percentage
            CameraBgState.White => RgbaFloat.White,
            CameraBgState.Custom => BackgroundColor,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }

    public void Resize(Vector2 size)
    {
        Size = size;

        Recreate();
    }

    public void Resize(IRenderable renderable)
    {
        Size = (Vector2)renderable.GetRenderSize(this);

        Recreate();
    }

    private void CalculateLightPosition(float cameraDistance)
    {
        float azimuth = SunAzimuth - 1.5707964F; // azimuth - 90deg
        sunPosition.X = cameraDistance * float.Cos(SunAltitude) * float.Cos(azimuth);
        sunPosition.Y = cameraDistance * float.Sin(SunAltitude);
        sunPosition.Z = cameraDistance * float.Cos(SunAltitude) * float.Sin(azimuth);
    }

    private void Recreate()
    {
        GraphicsDevice graphicsDevice = GuiManager.GraphicsDevice;

        // dispose completely halts graphics if we don't wait
        graphicsDevice.WaitForIdle();

        // Get rid of old resources
        ColorPass.Dispose();
        LightingPass.Dispose();
        RemovalPass.Dispose();

        // Framebuffers
        RemovalPass = new RenderPass(graphicsDevice.ResourceFactory, $"removal{Id}", BlendStateDescription.SingleAlphaBlend, Width, Height);
        LightingPass = new RenderPass(graphicsDevice.ResourceFactory, $"lighting{Id}", BlendStateDescription.SingleAlphaBlend, 4096, 4096);
        ColorPass = new RenderPass(graphicsDevice.ResourceFactory, $"color{Id}", BlendStateDescription.SingleAlphaBlend, Width, Height);

        const float nearDivisor = 5;
        float height = Size.Y / nearDivisor;
        float width = height * (Size.X / Size.Y);
        float cameraDistance = nearDivisor * 100 - StaticDepth;

        CalculateLightPosition(cameraDistance);

        LightingProjectionView = Matrix4x4.CreateLookAt(
            sunPosition * 5,
            Vector3.Zero,
            Vector3.UnitY
        ) * Matrix4x4.CreateOrthographicOffCenter(
            Width / 2f,
            -Width / 2f,
            -Height / 2f,
            Height / 2f,
            100,
            5000
        );

        ProjectionView = Matrix4x4.CreateLookAt(
            -Vector3.UnitZ * cameraDistance,
            Vector3.Zero,
            Vector3.UnitY
        ) * Utils.CreatePerspectiveOffsetProjection(
            width,
            height,
            100,
            float.PositiveInfinity,
            ProjectorOffset.X,
            ProjectorOffset.Y,
            cameraDistance + StaticDepth
        );

        GuiManager.GraphicsDevice.UpdateBuffer(_camBuffer, 0, new CameraUniform(this));
        GuiManager.GraphicsDevice.UpdateBuffer(_lightingBuffer, 0, new LightingUniform(this));

        WaitingForRecreate = false;
    }

    public void Render(IEnumerable<RenderDescription> activeRenderData)
    {
        if (WaitingForRecreate) Recreate();

        CommandList commandList = Scene.CommandList;

        // Clear removal
        commandList.SetFramebuffer(RemovalPass.Framebuffer);
        commandList.ClearColorTarget(0, RgbaFloat.Black);
        commandList.ClearDepthStencil(1, 0);

        // Clear shadows
        commandList.SetFramebuffer(LightingPass.Framebuffer);
        commandList.ClearColorTarget(0, Unlit ? RgbaFloat.Black : RgbaFloat.Red);
        commandList.ClearDepthStencil(Unlit ? 0 : 1, 0);

        // Clear objects
        commandList.SetFramebuffer(ColorPass.Framebuffer);
        commandList.ClearColorTarget(0, BackgroundColor);
        commandList.ClearDepthStencil(1, 0);

        foreach (RenderDescription desc in activeRenderData)
        {
            // Resize vertices if desc is bigger
            if (desc.Vertices.Length * 36 > _vtxBuffer.SizeInBytes)
            {
                _vtxBuffer.Dispose();
                _vtxBuffer = GuiManager.ResourceFactory.CreateBuffer(
                    new BufferDescription(
                        (uint)desc.Vertices.Length * 36,
                        BufferUsage.VertexBuffer
                    )
                );
            }

            // Resize indices if desc is bigger
            if (desc.Indices.Length * 4 > _idxBuffer.SizeInBytes)
            {
                _idxBuffer.Dispose();
                _idxBuffer = GuiManager.ResourceFactory.CreateBuffer(
                    new BufferDescription(
                        (uint)desc.Indices.Length * 4,
                        BufferUsage.IndexBuffer
                    )
                );
            }

            // Resize uniform buffer if desc is bigger
            if (desc.ObjectSizeInBytes > _objectBuffer.SizeInBytes)
            {
                _objectBuffer.Dispose();
                _objectBuffer = GuiManager.GraphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(
                        desc.ObjectSizeForBuffer,
                        BufferUsage.UniformBuffer
                    )
                );

                // Recreate the ResourceSet because it will be invalid as the uniform buffer will be disposed
                _infoResourceSet.Dispose();
                _infoResourceSet = GuiManager.GraphicsDevice.ResourceFactory.CreateResourceSet(
                    new ResourceSetDescription(
                        RWUtils.RWObjectDataLayout,
                        _camBuffer,
                        _passBuffer,
                        _lightingBuffer,
                        Scene.PaletteManager.PaletteInfo,
                        _objectBuffer,
                        GuiManager.GraphicsDevice.PointSampler
                    )
                );
            }

            commandList.UpdateBuffer(_vtxBuffer, 0, desc.Vertices);
            commandList.UpdateBuffer(_idxBuffer, 0, desc.Indices);

            commandList.SetVertexBuffer(0, _vtxBuffer);
            commandList.SetIndexBuffer(_idxBuffer, IndexFormat.UInt16);

            commandList.UpdateBuffer(_objectBuffer, 0, desc.ObjectData, (uint)desc.ObjectSizeInBytes);

            int passCounter = 0;

            foreach (RenderPass pass in desc.PassesBeforeEverything)
            {
                DrawPass(commandList, desc, passCounter++, pass);
            }

            if (desc.UseRemoval) DrawPass(commandList, desc, passCounter++, RemovalPass);
            if (desc.UseLighting) DrawPass(commandList, desc, passCounter++, LightingPass);
            if (desc.UseShaded) DrawPass(commandList, desc, passCounter++, ColorPass);

            foreach (RenderPass pass in desc.PassesAfterEverything)
            {
                DrawPass(commandList, desc, passCounter++, pass);
            }

            desc.Dispose();
        }
    }

    private void DrawPass(CommandList commandList, RenderDescription desc, int id, RenderPass pass, bool clear = false)
    {
        commandList.SetPipeline(EnsurePipelineForPass(GuiManager.ResourceFactory, desc.Id, pass, desc.Shaders, desc.Layouts));
        commandList.SetGraphicsResourceSet(0, _infoResourceSet);

        commandList.SetFramebuffer(pass.Framebuffer);

        if (clear)
        {
            commandList.ClearColorTarget(0, RgbaFloat.Clear);
            commandList.ClearDepthStencil(1, 0);
        }

        commandList.UpdateBuffer(_passBuffer, 0, new PassUniform(pass, id));

        if (desc.TextureSet != null) commandList.SetGraphicsResourceSet(1, desc.TextureSet);

        commandList.DrawIndexed((uint)desc.Indices.Length, 1, 0, 0, 0);
    }

    public void SaveToFile(string path)
    {
        GraphicsDevice graphicsDevice = GuiManager.GraphicsDevice;
        Texture objectTarget = ColorPass.RenderTexture.Texture;

        // Copy the color render target into a staging texture
        Texture copyTexture = graphicsDevice.ResourceFactory.CreateTexture(
            new TextureDescription(
                objectTarget.Width,
                objectTarget.Height,
                objectTarget.Depth,
                objectTarget.MipLevels,
                objectTarget.ArrayLayers,
                objectTarget.Format,
                TextureUsage.Staging,
                objectTarget.Type
            )
        );
        Fence fence = graphicsDevice.ResourceFactory.CreateFence(false);

        Scene.CommandList.Begin();
        Scene.CommandList.CopyTexture(objectTarget, copyTexture);
        Scene.CommandList.End();
        graphicsDevice.SubmitCommands(Scene.CommandList, fence);
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

    public Pipeline EnsurePipelineForPass(ResourceFactory factory, string objectId, RenderPass pass, Shader[] shaders, ResourceLayout[] layouts)
    {
        if (_pipelineCache.TryGetValue(objectId + pass.Id, out Pipeline pipeline)) return pipeline;

        pipeline = factory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                pass.BlendStateDescription,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(pass.FaceCullMode, PolygonFillMode.Solid, FrontFace.Clockwise, true, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(RWUtils.RWVertexLayout, shaders),
                layouts,
                pass.OutputDescription
            )
        );

        _pipelineCache.Add(objectId + pass.Id, pipeline);

        return pipeline;
    }

    public void Dispose()
    {
        GuiManager.GraphicsDevice.WaitForIdle();

        ColorPass.Dispose();
        LightingPass.Dispose();
        RemovalPass.Dispose();

        _vtxBuffer.Dispose();
        _idxBuffer.Dispose();

        GC.SuppressFinalize(this);
    }
}