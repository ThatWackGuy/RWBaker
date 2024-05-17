using System;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace RWBaker.Gui;

public struct FontRef : IDisposable
{
    public readonly string Name;
    public readonly ImFontConfigPtr ConfigPtr;
    public readonly ImFontPtr FontPtr;
    public readonly IntPtr MemoryRef;

    public FontRef(ImFontAtlasPtr atlas, byte[] bytes, string name, float size = 13)
    {
        string add = $", {size}px";

        if (name.Length > 40) name = name[..(40 - add.Length)];

        Name = name + add;

        unsafe
        {
            ImFontConfig* config = ImGuiNative.ImFontConfig_ImFontConfig();

            config->FontBuilderFlags = (uint)(FreeTypeBuilderFlags.ImGuiFreeTypeBuilderFlags_MonoHinting | FreeTypeBuilderFlags.ImGuiFreeTypeBuilderFlags_Monochrome);
            config->FontDataOwnedByAtlas = 0;

            for (int i = 0; i < Name.Length; i++)
            {
                config->Name[i] = (byte)Name[i];
            }

            ConfigPtr = new ImFontConfigPtr(config);
        }

        MemoryRef = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, MemoryRef, bytes.Length);

        FontPtr = atlas.AddFontFromMemoryTTF(MemoryRef, bytes.Length, size * 1.2f, ConfigPtr);
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(MemoryRef);
    }
}