using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using System.Runtime.InteropServices;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.RWObjects;
using RWBaker.Tiles;
using RWBaker.Windows;
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

    public static void Nav()
    {
        if (!ImGui.BeginMainMenuBar()) return;

        if (ImGui.MenuItem("Fetch All"))
        {
            GuiManager.AddWindow(new RWObjectFetcher());
        }

        if (ImGui.MenuItem("Palette Picker"))
        {
            GuiManager.AddWindow(new PalettePicker());
        }

        if (ImGui.BeginMenu("Render"))
        {
            if (ImGui.MenuItem("Render Single Tile"))
            {
                GuiManager.AddWindow(new RenderSingleTiles());
            }

            if (ImGui.MenuItem("Render All Tiles"))
            {
                GuiManager.AddWindow(new RenderAllTiles());
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("RWBaker Info"))
            {
                GuiManager.AddWindow(new InfoWindow());
            }

            if (ImGui.MenuItem("ImGui Info"))
            {
                GuiManager.AddWindow(new ImGuiInfoWindow());
            }

            if (ImGui.MenuItem("Credits"))
            {
                GuiManager.AddWindow(new CreditsWindow());
            }

            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    public static void PushStyleColor(ImGuiCol id, int r, int g, int b)
    {
        unsafe
        {
            float textAlpha = ImGui.GetStyleColorVec4(id)->W;
            ImGui.PushStyleColor(id, new Vector4(r / 255f, g / 255f, b / 255f, textAlpha));
        }
    }

    public static void PushStyleColor(ImGuiCol id, Vector3 col)
    {
        unsafe
        {
            float textAlpha = ImGui.GetStyleColorVec4(id)->W;
            ImGui.PushStyleColor(id, new Vector4(col / 255, textAlpha));
        }
    }

    public static void InfoMarker(ReadOnlySpan<char> text)
    {
        ImGui.TextDisabled("[?]");
        if (!ImGui.BeginItemTooltip()) return;
        ImGui.Text(text);
        ImGui.EndTooltip();
    }

    public static void WarningMarker(ReadOnlySpan<char> text)
    {
        PushStyleColor(ImGuiCol.Text, 230, 128, 32);
        ImGui.Text("[!]");
        ImGui.PopStyleColor();
        if (!ImGui.BeginItemTooltip()) return;
        PushStyleColor(ImGuiCol.Text, 252, 207, 3);
        ImGui.Text(text);
        ImGui.PopStyleColor();
        ImGui.EndTooltip();
    }
}