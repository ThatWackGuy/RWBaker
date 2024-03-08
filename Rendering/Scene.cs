using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.Props;
using RWBaker.RWObjects;
using RWBaker.Tiles;
using SixLabors.ImageSharp;
using Veldrid;

namespace RWBaker.Rendering;

public class Scene : IInspectable, IDisposable
{
    // TODO: Transfer the rendering to a Camera object. Leaving Scene as a container instead of both renderer and container.

    public RWObjectManager ObjectManager => Program.ObjectManager;
    public PaletteManager PaletteManager => Program.PaletteManager;

    private readonly Guid _id;
    public readonly string Name;

    public uint Width { get; private set; }
    public uint Height { get; private set; }

    private readonly GraphicsDevice graphicsDevice;

    private RgbaFloat RenderBg;

    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;

    private readonly CommandList commandList;

    public GuiTexture ObjectRender;
    public GuiTexture ShadowRender;
    private Texture renderObjectStencil;

    public Framebuffer ObjectFramebuffer { get; private set; }
    public Framebuffer ShadowFramebuffer { get; private set; }

    public DeviceBuffer ActiveSceneUniform { get; private set; }
    public DeviceBuffer ActiveObjectUniform { get; private set; }
    public ResourceSet ActiveResourceSet { get; private set; }

    public List<SceneObject> Objects { get; private set; } = new();
    public List<IRenderable> Renderables { get; private set; } = new();
    private RenderDescription[] activeRenderData = Array.Empty<RenderDescription>();

    public Vector2 ObjectOffset = Vector2.Zero;
    public Vector2 LightAngle = Vector2.Zero;

    public Matrix4x4 Projection { get; private set; }

    public int ShadowRepeat;
    public bool Unlit;
    public bool Rain;

    public Scene(uint width = 1, uint height = 1)
    {
        _id = Guid.NewGuid();
        Name = $"Scene {_id}";

        graphicsDevice = GuiManager.GraphicsDevice;

        ResourceFactory factory = GuiManager.ResourceFactory;

        RenderBg = RgbaFloat.Clear;

        // Draw Buffers
        vertexBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.VertexBuffer)); // 32 * const
        indexBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.IndexBuffer)); // 4 * const

        // Textures
        Texture objectTarget = factory.CreateTexture(
            new TextureDescription(
                width,
                height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        ObjectRender = GuiTexture.Create($"RWScene Object Texture {_id}", objectTarget);

        renderObjectStencil = factory.CreateTexture(
            new TextureDescription(
                width,
                height,
                1,
                1,
                1,
                PixelFormat.D24_UNorm_S8_UInt,
                TextureUsage.DepthStencil,
                TextureType.Texture2D
            )
        );
        renderObjectStencil.Name = $"RWScene Object Depth Texture {_id}";

        Texture shadowTarget = factory.CreateTexture(
            new TextureDescription(
                width,
                height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        ShadowRender = GuiTexture.Create($"RWScene Shadow Texture {_id}", shadowTarget);

        // Framebuffers
        ObjectFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, ObjectRender.Texture));
        ObjectFramebuffer.Name = $"Object Framebuffer {_id}";

        ShadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, ShadowRender.Texture));
        ShadowFramebuffer.Name = $"Shadow Framebuffer {_id}";

        Projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            width,
            height,
            0,
            1,
            -31
        );

        ActiveSceneUniform = factory.CreateBuffer(new BufferDescription(128, BufferUsage.UniformBuffer));
        ActiveObjectUniform = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        ActiveResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectDataLayout,
                ActiveSceneUniform,
                ActiveObjectUniform
            )
        );

        Width = width;
        Height = height;

        commandList = factory.CreateCommandList();
        ShadowRepeat = 1;
    }

    public void RenderInspector()
    {
        if (ImGui.Button("Add Tile")) ImGui.OpenPopup("addTile");

        if (ImGui.Button("Add Prop")) ImGui.OpenPopup("addProp");

        if (ImGui.BeginPopup("addTile"))
        {
            ImGui.InputTextWithHint("Search", "Type Words To Search", ref ObjectManager.TileLastSearched, 280);

            if (ImGui.BeginCombo("##tile_picker", "Select Tile to Add"))
            {
                foreach (TileInfo t in ObjectManager.Tiles.Where(t => t.SearchName.Contains(ObjectManager.TileLastSearched, StringComparison.CurrentCultureIgnoreCase)))
                {
                    Utils.PushStyleColor(ImGuiCol.Text, t.CategoryColor);
                    ImGui.Text("|");
                    ImGui.SameLine();
                    ImGui.PopStyleColor();

                    if (t.HasWarnings)
                    {
                        Utils.WarningMarker(t.Warnings);
                        ImGui.SameLine();
                    }

                    if (!ImGui.Selectable(t.ProperName)) continue;
                    if (!File.Exists(Path.Combine(ObjectManager.GraphicsDir, $"{t.Name}.png"))) continue;
                    AddObject(new TileObject(this, ObjectManager, t));
                    ObjectManager.TileLastUsed = t.ProperName;
                }

                ImGui.EndCombo();
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopup("addProp"))
        {
            ImGui.InputTextWithHint("Search", "Type Words To Search", ref ObjectManager.PropLastSearched, 280);
            if (ImGui.BeginCombo("##prop_picker", "Select Prop To Add"))
            {
                foreach (IProp p in ObjectManager.Props.Where(t => t.SearchName().Contains(ObjectManager.PropLastSearched, StringComparison.CurrentCultureIgnoreCase)))
                {
                    Utils.PushStyleColor(ImGuiCol.Text, p.CategoryColor());
                    ImGui.Text("|");
                    ImGui.SameLine();
                    ImGui.PopStyleColor();

                    if (p.HasWarnings())
                    {
                        Utils.WarningMarker(p.Warnings());
                        ImGui.SameLine();
                    }

                    if (!ImGui.Selectable(p.ProperName())) continue;
                    if (!File.Exists(Path.Combine(ObjectManager.PropsDir, $"{p.Name()}.png"))) continue;
                   AddObject(new PropObject(this, ObjectManager, p));
                   ObjectManager.PropLastUsed = p.ProperName();
                }

                ImGui.EndCombo();
            }

            ImGui.EndPopup();
        }

        ImGui.SliderInt("Shadow Smoothness", ref ShadowRepeat, 0, 50);

        if (ImGui.BeginCombo("##background", "Choose background"))
        {
            if (ImGui.Selectable("Clear"))
            {
                SetBackground(0);
            }
            else if (ImGui.Selectable("White"))
            {
                SetBackground(1);
            }
            else if (ImGui.Selectable("Sky"))
            {
                SetBackground(2);
            }

            ImGui.EndCombo();
        }

        ImGui.SliderFloat("Layer Offset X", ref ObjectOffset.X, -50, 50);
        ImGui.SliderFloat("Layer Offset Y", ref ObjectOffset.Y, -50, 50);
        if (ImGui.Button("Reset Layer Offset")) ObjectOffset = Vector2.Zero;

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.SliderFloat("Light Offset X", ref LightAngle.X, -50, 50);
        ImGui.SliderFloat("Light Offset Y", ref LightAngle.Y, -50, 50);
        if (ImGui.Button("Reset Light Offset")) LightAngle = Vector2.Zero;

        ImGui.Checkbox("Everything Unlit", ref Unlit);
        ImGui.Checkbox("Rain", ref Rain);

        if (ImGui.Button("Save Scene As Image"))
        {
            Directory.CreateDirectory("./Scenes/");
            SaveToFile($"./Scenes/{Name}.png");
        }
    }

    public void AddObject(SceneObject sceneObject)
    {
        Objects.Add(sceneObject);
        if (sceneObject is IRenderable renderable) Renderables.Add(renderable);
    }

    public void RemoveObject(SceneObject sceneObject)
    {
        Objects.Remove(sceneObject);
        if (sceneObject is IRenderable renderable) Renderables.Remove(renderable);
    }

    public void SetBackground(RgbaFloat color)
    {
        RenderBg = color;
    }

    public void SetBackground(int type)
    {
        RenderBg = type switch
        {
            0 => RgbaFloat.Clear,
            1 => RgbaFloat.White,
            2 => PaletteManager.CurrentPalette.Image[0, Rain ? 8 : 0].ToRgbaFloat(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private void RenderShadows(RenderDescription desc)
    {
        for (int i = 0; i < ShadowRepeat; i++)
        {
            commandList.UpdateBuffer(ActiveSceneUniform, 0, new RWSceneInfo(this, i, true));

            commandList.UpdateBuffer(vertexBuffer, 0, desc.Vertices);
            commandList.UpdateBuffer(indexBuffer, 0, desc.Indices);

            commandList.SetVertexBuffer(0, vertexBuffer);
            commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);

            commandList.SetPipeline(desc.Pipeline);
            commandList.SetGraphicsResourceSet(0, ActiveResourceSet); // check RWUniforms
            if (desc.HasTextureSet) commandList.SetGraphicsResourceSet(1, desc.TextureSet);

            commandList.DrawIndexed((uint)desc.Indices.Length, 1, 0, 0, 0);
        }
    }

    private void RenderObject(RenderDescription desc)
    {
        commandList.UpdateBuffer(vertexBuffer, 0, desc.Vertices);
        commandList.UpdateBuffer(indexBuffer, 0, desc.Indices);

        commandList.SetVertexBuffer(0, vertexBuffer);
        commandList.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);

        commandList.SetPipeline(desc.Pipeline);
        commandList.SetGraphicsResourceSet(0, ActiveResourceSet); // check RWUniforms
        if (desc.HasTextureSet) commandList.SetGraphicsResourceSet(1, desc.TextureSet);

        commandList.DrawIndexed((uint)desc.Indices.Length, 1, 0, 0, 0);
    }

    /**
     * Draws all currently registered <see cref="IRenderable"/>s in Scene.<see cref="Renderables"/>
     */
    public void Render()
    {
        activeRenderData = Renderables.Select(renderable => renderable.GetRenderDescription(this)).ToArray();

        commandList.Begin();

        commandList.SetFramebuffer(ShadowFramebuffer);
        commandList.ClearColorTarget(0, Unlit ? RgbaFloat.Black : RgbaFloat.Red);
        commandList.ClearDepthStencil(Unlit ? 0 : 1, 0);
        foreach (RenderDescription desc in activeRenderData)
        {
            // Resize vertices if desc is bigger
            if (desc.Vertices.Length * 36 > vertexBuffer.SizeInBytes)
            {
                vertexBuffer.Dispose();
                vertexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(
                        (uint)desc.Vertices.Length * 36,
                        BufferUsage.VertexBuffer
                    )
                );
            }

            // Resize indices if desc is bigger
            if (desc.Indices.Length * 4 > indexBuffer.SizeInBytes)
            {
                indexBuffer.Dispose();
                indexBuffer = graphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(
                        (uint)desc.Indices.Length * 4,
                        BufferUsage.IndexBuffer
                    )
                );
            }

            // Resize uniform buffer if desc is bigger
            if (desc.ObjectSizeInBytes > ActiveObjectUniform.SizeInBytes)
            {
                ActiveObjectUniform.Dispose();
                ActiveObjectUniform = graphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(
                        desc.ObjectSizeForBuffer,
                        BufferUsage.UniformBuffer
                    )
                );

                // Recreate the ResourceSet because it will be invalid as the uniform buffer will be disposed
                ActiveResourceSet.Dispose();
                ActiveResourceSet = graphicsDevice.ResourceFactory.CreateResourceSet(
                    new ResourceSetDescription(
                        RWUtils.RWObjectDataLayout,
                        ActiveSceneUniform,
                        ActiveObjectUniform
                    )
                );
            }

            commandList.UpdateBuffer(ActiveObjectUniform, 0, desc.ObjectData, (uint)desc.ObjectSizeInBytes);
            RenderShadows(desc);
        }

        commandList.UpdateBuffer(ActiveSceneUniform, 0, new RWSceneInfo(this, 0, false));

        commandList.SetFramebuffer(ObjectFramebuffer);
        commandList.ClearColorTarget(0, RenderBg);
        commandList.ClearDepthStencil(1, 0);
        foreach (RenderDescription desc in activeRenderData)
        {
            commandList.UpdateBuffer(ActiveObjectUniform, 0, desc.ObjectData, (uint)desc.ObjectSizeInBytes);
            RenderObject(desc);
            desc.Dispose();
        }

        commandList.End();

        // Submit and wait for completion
        graphicsDevice.SubmitCommands(commandList);
        graphicsDevice.WaitForIdle();
    }

    public void Resize(uint width, uint height)
    {
        Width = width;
        Height = height;

        Recreate(graphicsDevice.ResourceFactory);
    }

    public void Resize(Vector2 size)
    {
        Width = (uint)size.X;
        Height = (uint)size.Y;

        Recreate(graphicsDevice.ResourceFactory);
    }

    public void Resize(IRenderable renderable)
    {
        Vector2Int size = renderable.GetRenderSize(this);

        Width = (uint)size.X;
        Height = (uint)size.Y;

        Recreate(graphicsDevice.ResourceFactory);
    }

    private void Recreate(ResourceFactory factory)
    {
        // dispose completely halts graphics if we don't wait
        graphicsDevice.WaitForIdle();

        // Get rid of old resources
        ObjectFramebuffer.Dispose();
        ShadowFramebuffer.Dispose();

        ObjectRender.Dispose();
        ShadowRender.Dispose();
        renderObjectStencil.Dispose();

        // Recreate the textures
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
        ObjectRender = GuiTexture.Create($"RWScene Object Texture {_id}", objectTarget);

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
        renderObjectStencil.Name = $"RWScene Object Depth Texture {_id}";

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
        ShadowRender = GuiTexture.Create($"RWScene Shadow Texture {_id}", shadowTarget);

        // Recreate the framebuffers
        ObjectFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, ObjectRender.Texture));
        ObjectFramebuffer.Name = $"Object Framebuffer {_id}";

        ShadowFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(renderObjectStencil, ShadowRender.Texture));
        ShadowFramebuffer.Name = $"Shadow Framebuffer {_id}";

        Projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            Width,
            Height,
            0,
            1,
            -31
        );
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

        Objects.Clear();

        vertexBuffer.Dispose();
        indexBuffer.Dispose();

        commandList.Dispose();

        GC.SuppressFinalize(this);
    }
}