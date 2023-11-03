using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;
using RWBaker.PropTools;
using RWBaker.TileTools;
using SixLabors.ImageSharp;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace RWBaker;

public static class Program
{
    private static Sdl2Window window;
    private static CommandList CommandList;
    private static readonly RgbaFloat clearColor = new(0.15f, 0.15f, 0.15f, 0);
    
    public static readonly List<Window> Windows = new();
    public static readonly List<Window> WindowDelete = new();
    public static readonly List<Window> WindowAdd = new();
    
    public static readonly List<Tile> Tiles = new();
    
    public static readonly List<Prop> Props = new();

    public static void Main()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
        
        Context.LoadContext();
        Context context = Context.GetContext();
        
        // Create window
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(
                context.SavedWindowPos.X,
                context.SavedWindowPos.Y,
                context.SavedWindowSize.X,
                context.SavedWindowSize.Y,
                context.WindowState,
                "RWBaker"
            ),
            new GraphicsDeviceOptions(
                false,
                null,
                false,
                ResourceBindingModel.Improved,
                true,
                true
            ),
            GraphicsBackend.Vulkan,
            out window,
            out GraphicsDevice graphics
        );

        CommandList = graphics.ResourceFactory.CreateCommandList();
        Graphics.Load(graphics, graphics.MainSwapchain.Framebuffer.OutputDescription, window);
        
        // Load Icon Texture
        Texture iconTex = Graphics.TextureFromResource("res.bakertex.png");
        Graphics.TryCreateImGuiTexture("_icon", iconTex, out _);
        
        Palette.Load();

        if (context.SavedGraphicsDir != "")
        {
            RWUtils.GetTiles(out string tileLog);
            Console.WriteLine($"TILE LOAD LOG:\n{tileLog}\n\n");
        }
        
        RWUtils.LoadGraphicsResources();

        window.Resized += () =>
        {
            Graphics.GraphicsDevice.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            Graphics.WindowResized(window.Width, window.Height);
        };

        window.Moved += p =>
        {
            Graphics.WindowMoved(p.X, p.Y);
        };

        window.Closing += () =>
        {
            context.WindowState = window.WindowState;
            context.SavedWindowPos.X = Graphics.WindowPosX;
            context.SavedWindowPos.Y = Graphics.WindowPosY;
            context.SavedWindowSize.X = Graphics.WindowWidth;
            context.SavedWindowSize.Y = Graphics.WindowHeight;
            Context.SaveContext();
        };

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (window.Exists)
        {
            float time = stopwatch.ElapsedTicks / (float)Stopwatch.Frequency;
            stopwatch.Restart();
            InputSnapshot snapshot = window.PumpEvents();
            if (!window.Exists) break;

            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
            Graphics.UpdateImGui(time, snapshot);
            Utils.Nav();

            // Add pending windows
            foreach (Window w in WindowAdd)
            {
                Windows.Add(w);
            }

            WindowAdd.Clear();

            // Delete windows marked for deletion
            foreach (Window w in WindowDelete)
            {
                Windows.Remove(w);
                w.Dispose();
            }

            WindowDelete.Clear();

            // Update remaining windows
            foreach (Window w in Windows)
            {
                w.Update();

                // if marked for deletion add to removal list
                if (w.MarkedForDeletion) WindowDelete.Add(w);
            }

            // I hate graphics
            CommandList.Begin();
            CommandList.SetFramebuffer(graphics.MainSwapchain.Framebuffer);
            CommandList.ClearColorTarget(0, clearColor);
            Graphics.Render(CommandList);
            CommandList.End();
            graphics.SubmitCommands(CommandList);
            graphics.SwapBuffers(graphics.MainSwapchain);
        }

        graphics.WaitForIdle();
        Graphics.Dispose();
        CommandList.Dispose();
        graphics.Dispose();
    }
}