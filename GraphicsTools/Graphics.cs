using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;

namespace RWBaker.GraphicsTools;

public static class Graphics
{
    public static GraphicsDevice GraphicsDevice { get; private set; }
    public static ResourceFactory ResourceFactory  { get; private set; }

    private static bool frameBegun;

    private static OutputDescription outputDescription;

    private static DeviceBuffer vertexBuffer;
    private static DeviceBuffer indexBuffer;
    private static DeviceBuffer projMatrixBuffer;

    private static Texture fontTexture;
    private static TextureView fontTextureView;

    private static Shader[] mainShaders;
    
    public static ResourceLayout TextureLayout;
    private static ResourceLayout projectionLayout;

    private static Pipeline pipeline;
    
    private static ResourceSet mainResourceSet;
    private static ResourceSet fontResourceSet;

    private const IntPtr fontAtlasID = 1;

    public static int WindowWidth { get; private set; }
    public static int WindowHeight { get; private set; }
        
    public static int WindowPosX { get; private set; }
    public static int WindowPosY { get; private set; }
        
    private static readonly Vector2 scaleFactor = Vector2.One;
    
    // :3
    public static Texture EmptyTexture;

    public static void Load(GraphicsDevice graphics, OutputDescription outDesc, Sdl2Window window)
    {
        GraphicsDevice = graphics;
        ResourceFactory = graphics.ResourceFactory;
        
        outputDescription = outDesc;

        WindowWidth = window.Width;
        WindowHeight = window.Height;
        WindowPosX = window.X;
        WindowPosY = window.Y;
        
        ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;
        io.ConfigDockingWithShift = true;
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        
        CreateDeviceResources();
        SetPerFrameImGuiData(1f / 60f);
        
        ImGui.NewFrame();
        frameBegun = true;
    }

    public static void WindowResized(int width, int height)
    {
        WindowWidth = width;
        WindowHeight = height;
    }
        
    public static void WindowMoved(int x, int y)
    {
        WindowPosX = x;
        WindowPosY = y;
    }
    
    public static void CreateDeviceResources()
    {
        vertexBuffer = ResourceFactory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        indexBuffer = ResourceFactory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        RecreateFontDeviceTexture();

        projMatrixBuffer = ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        
        ShaderDescription guiVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.vertex.spv"), "main");
        ShaderDescription guiFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.fragment.spv"), "main");
        mainShaders = ResourceFactory.CreateFromSpirv(guiVert, guiFrag);

        VertexLayoutDescription[] vertexLayouts =
        {
            new(
                new VertexElementDescription("in_position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4_Norm)
            )
        };
        
        projectionLayout = ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            )
        );
        
        TextureLayout = ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
            )
        );

        GraphicsPipelineDescription pipelineDesc = new(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, mainShaders),
            new[] { projectionLayout, TextureLayout },
            outputDescription,
            ResourceBindingModel.Improved
        );
        
        pipeline = ResourceFactory.CreateGraphicsPipeline(ref pipelineDesc);

        mainResourceSet = ResourceFactory.CreateResourceSet(new ResourceSetDescription(projectionLayout, projMatrixBuffer, GraphicsDevice.PointSampler));

        fontResourceSet = ResourceFactory.CreateResourceSet(new ResourceSetDescription(TextureLayout, fontTextureView));

        EmptyTexture = ResourceFactory.CreateTexture(
            new TextureDescription(
                1,
                1,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
    }

    public static void UpdateTextureFromImage(Texture texture, Image<Rgba32> image)
    {
        unsafe
        {
            if (!image.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> mem))
            {
                throw new Exception("MEMORY NOT AVAILABLE");
            }
            
            using MemoryHandle handle = mem.Pin();
            
            GraphicsDevice.UpdateTexture(
                texture,
                (IntPtr)handle.Pointer,
                (uint)(4 * image.Width * image.Height),
                0,
                0,
                0,
                (uint)image.Width,
                (uint)image.Height,
                1,
                0,
                0
            );
            
            handle.Dispose();
        }
    }

    public static Texture TextureFromImage(Image<Rgba32> image)
    {
        Texture texture = ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        
        UpdateTextureFromImage(texture, image);

        return texture;
    }
    
    public static Texture TextureFromImage(string imagePath)
    {
        Image<Rgba32> image = Image.Load<Rgba32>(imagePath);

        Texture texture = ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        
        UpdateTextureFromImage(texture, image);

        return texture;
    }
    
    public static Texture TextureFromResource(string path)
    {
        Image<Rgba32> image = Image.Load<Rgba32>(Utils.GetEmbeddedBytes(path));

        Texture texture = ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        
        UpdateTextureFromImage(texture, image);

        return texture;
    }
    
    public static void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        
        io.Fonts.SetTexID(fontAtlasID);

        fontTexture = ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
        
        GraphicsDevice.UpdateTexture(
            fontTexture,
            pixels,
            (uint)(bytesPerPixel * width * height),
            0,
            0,
            0,
            (uint)width,
            (uint)height,
            1,
            0,
            0);
        
        fontTextureView = ResourceFactory.CreateTextureView(fontTexture);

        io.Fonts.ClearTexData();
    }
    
    // Below is dedicated to ImGui
    // DO NOT TOUCH, BLACK MAGIC
    public static void Render(CommandList cl)
    {
        frameBegun = false;
        ImGui.Render();
        RenderImGui(ImGui.GetDrawData(), cl);
    }
    
    public static void UpdateImGui(float deltaSeconds, InputSnapshot snapshot)
    {
        if (frameBegun)
        {
            ImGui.Render();
        }
        
        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput(snapshot);

        frameBegun = true;
        ImGui.NewFrame();
    }
    
    private static void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(
            WindowWidth / scaleFactor.X,
            WindowHeight / scaleFactor.Y);
        io.DisplayFramebufferScale = scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    private static bool TryMapKey(Key key, out ImGuiKey result)
    {
        ImGuiKey KeyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiKey startKey2)
        {
            int changeFromStart1 = (int)keyToConvert - (int)startKey1;
            return startKey2 + changeFromStart1;
        }

        result = key switch
        {
            >= Key.F1 and <= Key.F12 => KeyToImGuiKeyShortcut(key, Key.F1, ImGuiKey.F1),
            >= Key.Keypad0 and <= Key.Keypad9 => KeyToImGuiKeyShortcut(key, Key.Keypad0, ImGuiKey.Keypad0),
            >= Key.A and <= Key.Z => KeyToImGuiKeyShortcut(key, Key.A, ImGuiKey.A),
            >= Key.Number0 and <= Key.Number9 => KeyToImGuiKeyShortcut(key, Key.Number0, ImGuiKey._0),
            Key.ShiftLeft or Key.ShiftRight => ImGuiKey.ModShift,
            Key.ControlLeft or Key.ControlRight => ImGuiKey.ModCtrl,
            Key.AltLeft or Key.AltRight => ImGuiKey.ModAlt,
            Key.WinLeft or Key.WinRight => ImGuiKey.ModSuper,
            Key.Menu => ImGuiKey.Menu,
            Key.Up => ImGuiKey.UpArrow,
            Key.Down => ImGuiKey.DownArrow,
            Key.Left => ImGuiKey.LeftArrow,
            Key.Right => ImGuiKey.RightArrow,
            Key.Enter => ImGuiKey.Enter,
            Key.Escape => ImGuiKey.Escape,
            Key.Space => ImGuiKey.Space,
            Key.Tab => ImGuiKey.Tab,
            Key.BackSpace => ImGuiKey.Backspace,
            Key.Insert => ImGuiKey.Insert,
            Key.Delete => ImGuiKey.Delete,
            Key.PageUp => ImGuiKey.PageUp,
            Key.PageDown => ImGuiKey.PageDown,
            Key.Home => ImGuiKey.Home,
            Key.End => ImGuiKey.End,
            Key.CapsLock => ImGuiKey.CapsLock,
            Key.ScrollLock => ImGuiKey.ScrollLock,
            Key.PrintScreen => ImGuiKey.PrintScreen,
            Key.Pause => ImGuiKey.Pause,
            Key.NumLock => ImGuiKey.NumLock,
            Key.KeypadDivide => ImGuiKey.KeypadDivide,
            Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
            Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
            Key.KeypadAdd => ImGuiKey.KeypadAdd,
            Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
            Key.KeypadEnter => ImGuiKey.KeypadEnter,
            Key.Tilde => ImGuiKey.GraveAccent,
            Key.Minus => ImGuiKey.Minus,
            Key.Plus => ImGuiKey.Equal,
            Key.BracketLeft => ImGuiKey.LeftBracket,
            Key.BracketRight => ImGuiKey.RightBracket,
            Key.Semicolon => ImGuiKey.Semicolon,
            Key.Quote => ImGuiKey.Apostrophe,
            Key.Comma => ImGuiKey.Comma,
            Key.Period => ImGuiKey.Period,
            Key.Slash => ImGuiKey.Slash,
            Key.BackSlash or Key.NonUSBackSlash => ImGuiKey.Backslash,
            _ => ImGuiKey.None
        };

        return result != ImGuiKey.None;
    }

    private static void UpdateImGuiInput(InputSnapshot snapshot)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.AddMousePosEvent(snapshot.MousePosition.X, snapshot.MousePosition.Y);
        io.AddMouseButtonEvent(0, snapshot.IsMouseDown(MouseButton.Left));
        io.AddMouseButtonEvent(1, snapshot.IsMouseDown(MouseButton.Right));
        io.AddMouseButtonEvent(2, snapshot.IsMouseDown(MouseButton.Middle));
        io.AddMouseButtonEvent(3, snapshot.IsMouseDown(MouseButton.Button1));
        io.AddMouseButtonEvent(4, snapshot.IsMouseDown(MouseButton.Button2));
        io.AddMouseWheelEvent(0f, snapshot.WheelDelta);
        foreach (char t in snapshot.KeyCharPresses)
        {
            io.AddInputCharacter(t);
        }

        foreach (KeyEvent keyEvent in snapshot.KeyEvents)
        {
            if (TryMapKey(keyEvent.Key, out ImGuiKey key))
            {
                io.AddKeyEvent(key, keyEvent.Down);
            }
        }
    }

    private static void RenderImGui(ImDrawDataPtr draw_data, CommandList cl)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (draw_data.CmdListsCount == 0)
        {
            return;
        }

        uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVBSize > vertexBuffer.SizeInBytes)
        {
            GraphicsDevice.DisposeWhenIdle(vertexBuffer);
            vertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > indexBuffer.SizeInBytes)
        {
            GraphicsDevice.DisposeWhenIdle(indexBuffer);
            indexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            cl.UpdateBuffer(
                vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmd_list.VtxBuffer.Data,
                (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            cl.UpdateBuffer(
                indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmd_list.IdxBuffer.Data,
                (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into the constant buffer
        // (Moving 0,0 to the top left lol)
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
            0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        GraphicsDevice.UpdateBuffer(projMatrixBuffer, 0, ref mvp);

        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(pipeline);
        cl.SetGraphicsResourceSet(0, mainResourceSet);

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        int vtxOffset = 0;
        int idxOffset = 0;
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr cmd = cmd_list.CmdBuffer[cmd_i];

                if (cmd.TextureId != IntPtr.Zero)
                {
                    cl.SetGraphicsResourceSet(1,
                        cmd.TextureId == fontAtlasID
                            ? fontResourceSet
                            : GuiTexture.GetTexture(cmd.TextureId).ResourceSet);
                }
                
                cl.SetScissorRect(
                    0,
                    (uint)cmd.ClipRect.X,
                    (uint)cmd.ClipRect.Y,
                    (uint)(cmd.ClipRect.Z - cmd.ClipRect.X),
                    (uint)(cmd.ClipRect.W - cmd.ClipRect.Y));

                cl.DrawIndexed(cmd.ElemCount, 1, cmd.IdxOffset + (uint)idxOffset, (int)cmd.VtxOffset + vtxOffset, 0);
            }
            
            vtxOffset += cmd_list.VtxBuffer.Size;
            idxOffset += cmd_list.IdxBuffer.Size;
        }
    }
    
    public static void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        projMatrixBuffer.Dispose();
        fontTexture.Dispose();
        fontTextureView.Dispose();
        projectionLayout.Dispose();
        TextureLayout.Dispose();
        pipeline.Dispose();
        mainResourceSet.Dispose();

        foreach (Shader shader in mainShaders)
        {
            shader.Dispose();
        }
    }
}