using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using RWBaker.GeneralTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace RWBaker.GraphicsTools;

public static class GuiManager
{
    public static GraphicsDevice GraphicsDevice { get; private set; }
    public static ResourceFactory ResourceFactory  { get; private set; }

    private static bool frameBegun;
    
    public static Sdl2Window Window;
    public static CommandList CommandList;
    private static readonly RgbaFloat clearColor = new(0.15f, 0.15f, 0.15f, 0);

    private static OutputDescription outputDescription;

    private static DeviceBuffer vertexBuffer;
    private static DeviceBuffer indexBuffer;
    private static DeviceBuffer projMatrixBuffer;

    private static GuiTexture fontTex;

    private static Shader[] mainShaders;
    
    public static ResourceLayout TextureLayout;
    private static ResourceLayout projectionLayout;

    private static Pipeline pipeline;
    
    private static ResourceSet mainResourceSet;
    
    public static int WindowWidth { get; private set; }
    public static int WindowHeight { get; private set; }
        
    public static int WindowPosX { get; private set; }
    public static int WindowPosY { get; private set; }
        
    private static readonly Vector2 scaleFactor = Vector2.One;
    
    // Windows
    private static readonly List<Window> windows = new();
    public static ReadOnlyCollection<Window> Windows => windows.AsReadOnly();

    private static readonly List<Window> windowsToDelete = new();
    private static readonly List<Window> windowsToAdd = new();

    public static void Load(Context context)
    {
        // no.
        if (context.WindowState == WindowState.Hidden) context.WindowState = WindowState.Normal;
        
        // Create the window
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(
                context.SavedWindowPos.X,
                context.SavedWindowPos.Y,
                context.SavedWindowSize.X,
                context.SavedWindowSize.Y,
                context.WindowState == WindowState.Minimized ? WindowState.Normal : context.WindowState,
                "RWBaker"
            ),
            new GraphicsDeviceOptions(
                context.GraphicsDebug,
                null,
                context.VSync,
                ResourceBindingModel.Improved,
                true,
                true
            ),
            GraphicsBackend.Vulkan,
            out Window,
            out GraphicsDevice graphics
        );

        // with a minimised or hidden window at startup a swapchain cannot be created
        // leading to vkAcquireNextImageKHR giving out an annoyingly vague memory exception
        // this line, the state sterilisation and the ternary checks above make sure that never happens
        if (context.WindowState == WindowState.Minimized) Window.WindowState = WindowState.Minimized;

        CommandList = graphics.ResourceFactory.CreateCommandList();
        
        Window.Resized += () =>
        {
            GraphicsDevice.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
            WindowWidth = Window.Width;
            WindowHeight = Window.Height;
        };

        Window.Moved += p =>
        {
            WindowPosX = p.X;
            WindowPosY = p.Y;
        };

        Window.Closing += () =>
        {
            context.WindowState = Window.WindowState;
            context.SavedWindowPos.X = WindowPosX;
            context.SavedWindowPos.Y = WindowPosY;
            context.SavedWindowSize.X = WindowWidth;
            context.SavedWindowSize.Y = WindowHeight;
            Program.Context.Save("./userdata.json");
        };
        
        GraphicsDevice = graphics;
        ResourceFactory = graphics.ResourceFactory;
        
        outputDescription = graphics.MainSwapchain.Framebuffer.OutputDescription;

        WindowWidth = Window.Width;
        WindowHeight = Window.Height;
        WindowPosX = Window.X;
        WindowPosY = Window.Y;
        
        ImGui.CreateContext();
        ImGuiIOPtr io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        io.Fonts.Flags |= ImFontAtlasFlags.NoBakedLines;
        io.ConfigDockingWithShift = true;
        io.ConfigWindowsMoveFromTitleBarOnly = true;
        io.ConfigDockingTransparentPayload = true;
        
        CreateDeviceResources();
        RecreateFonts();
        SetPerFrameImGuiData(1f / 60f);
        
        ImGui.NewFrame();
        frameBegun = true;
    }

    public static void RenderProcess()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (Window.Exists)
        {
            float time = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
            stopwatch.Restart();
            InputSnapshot snapshot = Window.PumpEvents();
            if (!Window.Exists) break;

            UpdateImGui(time, snapshot);
            Utils.Nav();
            
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

            if (windowsToDelete.Count > 0)
            {
                // Delete windows marked for deletion
                foreach (Window w in windowsToDelete)
                {
                    windows.Remove(w);
                    w.Dispose();
                }

                windowsToDelete.Clear();
            }

            if (windowsToAdd.Count > 0)
            {
                // Add pending windows
                foreach (Window w in windowsToAdd)
                {
                    windows.Add(w);
                }

                windowsToAdd.Clear();
            }

            // Update remaining windows
            foreach (Window w in windows)
            {
                try
                {
                    w.Update();
                }
                catch (Exception e)
                {
                    RemoveWindow(w);
                    Exception(e);
                }
            }

            CommandList.Begin();
            CommandList.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
            CommandList.ClearColorTarget(0, clearColor);
            Render(CommandList);
            CommandList.End();
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);
            GraphicsDevice.WaitForIdle();
        }
        
        Dispose();
    }

    public static void CreateDeviceResources()
    {
        vertexBuffer = ResourceFactory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        indexBuffer = ResourceFactory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        
        projMatrixBuffer = ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        
        ShaderDescription guiVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.vertex.spv"), "main");
        ShaderDescription guiFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.fragment.spv"), "main");
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
    }
    
    // TODO: Custom font support later?
    public static void RecreateFonts()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        Texture texture = ResourceFactory.CreateTexture(
            TextureDescription.Texture2D(
                (uint)width,
                (uint)height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled
            )
        );

        GraphicsDevice.UpdateTexture(
            texture,
            pixels,
            (uint)(bytesPerPixel * width * height),
            0,
            0,
            0,
            (uint)width,
            (uint)height,
            1,
            0,
            0
        );
        
        fontTex = GuiTexture.Create("_default_font", texture);

        io.Fonts.SetTexID(fontTex.Index);
        
        io.Fonts.ClearTexData();
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

    public static void AddWindow(Window window) 
    {
        if (Windows.Any(w => w.InternalIdentifier == window.InternalIdentifier)) return;

        window.Open = true;
        windowsToAdd.Add(window);
    }

    public static void RemoveWindow(Window window)
    {
        if (Windows.All(w => w.InternalIdentifier != window.InternalIdentifier)) return;
        
        windowsToDelete.Add(window);
    }

    public static void Exception(Exception e)
    {
        AddWindow(new ExceptionWindow(e));
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
    
    private static ImGuiKey KeyToImGuiKeyShortcut(Key keyToConvert, Key startKey1, ImGuiKey startKey2)
    {
        int changeFromStart1 = (int)keyToConvert - (int)startKey1;
        return startKey2 + changeFromStart1;
    }

    private static bool TryMapKey(Key key, out ImGuiKey result)
    {
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
            ImDrawListPtr cmd_list = draw_data.CmdLists[i];

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
            ImDrawListPtr cmd_list = draw_data.CmdLists[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr cmd = cmd_list.CmdBuffer[cmd_i];

                if (cmd.TextureId != IntPtr.Zero)
                {
                    cl.SetGraphicsResourceSet(1,
                        cmd.TextureId == fontTex.Index
                            ? fontTex.ResourceSet
                            : GuiTexture.GetTexture(cmd.TextureId).ResourceSet
                    );
                }
                
                cl.SetScissorRect(
                    0,
                    (uint)cmd.ClipRect.X,
                    (uint)cmd.ClipRect.Y,
                    (uint)(cmd.ClipRect.Z - cmd.ClipRect.X),
                    (uint)(cmd.ClipRect.W - cmd.ClipRect.Y)
                );

                cl.DrawIndexed(cmd.ElemCount, 1, cmd.IdxOffset + (uint)idxOffset, (int)cmd.VtxOffset + vtxOffset, 0);
            }
            
            vtxOffset += cmd_list.VtxBuffer.Size;
            idxOffset += cmd_list.IdxBuffer.Size;
        }
    }
    
    public static void Dispose()
    {
        GraphicsDevice.WaitForIdle();
        CommandList.Dispose();
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        projMatrixBuffer.Dispose();
        projectionLayout.Dispose();
        TextureLayout.Dispose();
        pipeline.Dispose();
        mainResourceSet.Dispose();
        
        GuiTexture.DisposeAllTextures();

        foreach (Shader shader in mainShaders)
        {
            shader.Dispose();
        }
        
        GraphicsDevice.Dispose();
    }
}