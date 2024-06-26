using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using System.Runtime.InteropServices;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.Props;
using RWBaker.Tiles;
using RWBaker.Windows;
using Veldrid;

namespace RWBaker;

public static class Utils
{
    // common colors in uint
    public const uint IM_RED = 4278190335;
    public const uint IM_GREEN = 4278255360;
    public const uint IM_BLUE = 4294901760;
    public const uint IM_WHITE = 4294967295;
    public const uint IM_GRAY = 4286611584;

    public static byte[] ToBytes(object obj, int size)
    {
        byte[] bytes = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(obj, ptr, false);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);

        return bytes;
    }

    public static T FromBytes<T>(byte[] bytes)
    {
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        T? obj = (T?)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);

        if (obj == null) throw new NullReferenceException("Deserialized object was null");

        return obj;
    }

    public static void ChangePriority<T>(this List<T> list, T obj, int move)
    {
        int index = list.IndexOf(obj);

        T item = list[index];
        list.RemoveAt(index);
        list.Insert(int.Clamp(index + move, 0, list.Count), item);
    }

    public static DeviceBuffer CreateStructBuffer<T>(this ResourceFactory factory) where T : unmanaged
    {
        return factory.CreateBuffer(
            new BufferDescription(
                16 * (uint)float.Ceiling(Marshal.SizeOf<T>() / 16f),
                BufferUsage.UniformBuffer
            )
        );
    }

    public static Matrix4x4 CreatePerspectiveOffsetProjection(float width, float height, float near, float far, float xOffset, float yOffset, float staticLayerDistance)
    {
        float perW = width / 2;
        float perH = height / 2;

        Matrix4x4 defMatrix = Matrix4x4.CreatePerspectiveOffCenter(perW, -perW, -perH, perH, near, far);

        defMatrix.M31 = xOffset / -(2 * perW);
        defMatrix.M32 = -yOffset / (2 * perH);

        defMatrix.M41 = staticLayerDistance * defMatrix.M31;
        defMatrix.M42 = staticLayerDistance * defMatrix.M32;

        return defMatrix;
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

        if (ImGui.MenuItem("Scene Builder"))
        {
            GuiManager.AddWindow(new SceneBuilder());
        }

        if (ImGui.BeginMenu("Render"))
        {
            ImGui.SeparatorText("TILES");

            if (ImGui.MenuItem("Render Tiles"))
            {
                GuiManager.AddWindow(new RenderSingleTiles());
            }

            ImGui.SeparatorText("PROPS");

            if (ImGui.MenuItem("Render Single Prop"))
            {
                GuiManager.AddWindow(new RenderProps());
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

        if (GuiManager.DebugMode && ImGui.BeginMenu("Debug"))
        {
            if (ImGui.MenuItem("Resource Viewer"))
            {
                GuiManager.AddWindow(new ResourceViewer());
            }

            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    public static unsafe void PushStyleColor(ImGuiCol id, int r, int g, int b)
    {
        float textAlpha = ImGui.GetStyleColorVec4(id)->W;
        ImGui.PushStyleColor(id, new Vector4(r / 255f, g / 255f, b / 255f, textAlpha));
    }

    public static unsafe void PushStyleColor(ImGuiCol id, Vector3 col)
    {
        float textAlpha = ImGui.GetStyleColorVec4(id)->W;
        ImGui.PushStyleColor(id, new Vector4(col / 255, textAlpha));
    }

    public static bool SelectableTreeNode(ReadOnlySpan<char> fmt)
    {
        ImGui.Bullet();
        return ImGui.Selectable(fmt);
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
