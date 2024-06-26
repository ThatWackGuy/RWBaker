using System;
using ImGuiNET;

namespace RWBaker.Gui;

public struct FontRef
{
    private static readonly byte[] iconBytes = Utils.GetEmbeddedBytes("res.codicon.ttf");

    public readonly string Name;
    public readonly ImFontConfigPtr ConfigPtr;
    public readonly ImFontPtr FontPtr;

    public unsafe FontRef(ImFontAtlasPtr atlas, byte[] bytes, string name, float size = 16)
    {
        string add = $", {size}px";

        if (name.Length > 40) name = name[..(40 - add.Length)];

        Name = name + add;

        ImFontConfig* config = ImGuiNative.ImFontConfig_ImFontConfig();

        for (int i = 0; i < Name.Length; i++)
        {
            config->Name[i] = (byte)Name[i];
        }

        ConfigPtr = new ImFontConfigPtr(config);

        fixed (byte* fontPtr = bytes)
        {
            FontPtr = atlas.AddFontFromMemoryTTF(new IntPtr(fontPtr), bytes.Length, size, ConfigPtr);
        }

        ImFontConfig* iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();

        iconConfig->MergeMode = 1;
        iconConfig->PixelSnapH = 1;
        iconConfig->GlyphMinAdvanceX = size;

        ushort[] ranges = [Codicons.IconMin, Codicons.IconMax16, 0];

        fixed (ushort* iconRanges = ranges)
        {
            iconConfig->GlyphRanges = iconRanges;
        }

        const string iconsName = "Codicons";
        for (int i = 0; i < iconsName.Length; i++)
        {
            iconConfig->Name[i] = (byte)iconsName[i];
        }

        fixed (byte* fontPtr = iconBytes)
        {
            atlas.AddFontFromMemoryTTF(new IntPtr(fontPtr), iconBytes.Length, size, iconConfig);
        }
    }

    public static unsafe void MergeIcons(ImFontAtlasPtr atlas, float size)
    {
        ImFontConfig* iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();

        iconConfig->MergeMode = 1;
        iconConfig->PixelSnapH = 1;
        iconConfig->GlyphMinAdvanceX = size;

        ushort[] ranges = [Codicons.IconMin, Codicons.IconMax16, 0];

        fixed (ushort* iconRanges = ranges)
        {
            iconConfig->GlyphRanges = iconRanges;
        }

        const string iconsName = "Codicons";
        for (int i = 0; i < iconsName.Length; i++)
        {
            iconConfig->Name[i] = (byte)iconsName[i];
        }

        fixed (byte* fontPtr = iconBytes)
        {
            atlas.AddFontFromMemoryTTF(new IntPtr(fontPtr), iconBytes.Length, size, iconConfig);
        }
    }
}