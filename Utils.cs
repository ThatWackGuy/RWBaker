using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ImGuiNET;
using System.Runtime.InteropServices;
using RWBaker.GeneralTools;
using RWBaker.TileTools;
using Veldrid;

namespace RWBaker;

public static class Utils
{
    public static DeviceBuffer CreateStructBuffer<T>(this ResourceFactory factory) where T : unmanaged
    {
        return factory.CreateBuffer(
            new BufferDescription(
                16 * (uint)float.Ceiling(Marshal.SizeOf<T>() / 16f),
                BufferUsage.UniformBuffer
            )
        );
    }
    
    public static byte[] GetEmbeddedBytes(string path)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? resource = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + path);

        if (resource is null)
        {
            throw new Exception($"Couldn't get resource with path '{path}' in current assembly '{assembly.FullName}'");
        }

        byte[] bytes = new byte[resource.Length];
        resource.Read(bytes, 0, bytes.Length);
        
        return bytes;
    }

    public static void TryAddWindow<T>(T window) where T : Window
    {
        if (Program.Windows.Any(w => w is T)) return;
        
        Program.WindowAdd.Add(window);
    }

    public static void Nav()
    {
        if (!ImGui.BeginMainMenuBar()) return;
        
        if (ImGui.BeginMenu("Main"))
        {
            if (ImGui.MenuItem("Get Tiles"))
            {
                TryAddWindow(new TileLoader());
            }
            
            if (ImGui.MenuItem("Get Props"))
            {
                // Program.WindowAdd.Add(new PropTools.PropLoader());
            }
            
            if (ImGui.MenuItem("Select Palettes"))
            {
                TryAddWindow(new PalettePicker());
            }

            ImGui.EndMenu();
        }

        if (Program.Tiles.Count > 0 && ImGui.BeginMenu("Tile Tools"))
        {
            if (ImGui.MenuItem("Render Single Tile"))
            {
                TryAddWindow(new RenderSingleTiles());
            }
            
            if (ImGui.MenuItem("Render All Tiles"))
            {
                TryAddWindow(new RenderAllTiles());
            }

            ImGui.EndMenu();
        }
        
        if (Program.Tiles.Count > 0 && ImGui.BeginMenu("Prop Tools"))
        {
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("RWBaker Info"))
            {
                TryAddWindow(new InfoWindow());
            }
            
            if (ImGui.MenuItem("ImGui Info"))
            {
                TryAddWindow(new ImGuiInfoWindow());
            }
            
            if (ImGui.MenuItem("Credits"))
            {
                TryAddWindow(new CreditsWindow());
            }
            
            ImGui.EndMenu();
        }
            
        ImGui.EndMainMenuBar();
    }

    public static void InfoMarker(ReadOnlySpan<char> text)
    {
        ImGui.TextDisabled("[?]");
        if (!ImGui.BeginItemTooltip()) return;
        ImGui.Text(text);
        ImGui.EndTooltip();
    }
}