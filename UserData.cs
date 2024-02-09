using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker;

/// <summary>
/// Data from the program to be saved or loaded
/// </summary>
[Serializable]
public class UserData
{
    private static JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        IncludeFields = true,
        WriteIndented = true,
        IgnoreReadOnlyProperties = true
    };

    // Window options
    public Vector2Int SavedWindowPos;
    public Vector2Int SavedWindowSize;
    public WindowState WindowState;
    public Vector2Int ScalingFactor;
    public bool VSync;
    public bool DebugGraphics;

    // Last palette info
    public string SavedPaletteDir;
    public string UsingPalette1;
    public string UsingPalette2;
    public int PaletteBlend;

    // Last effect colors info
    public int EffectColorA;
    public int EffectColorB;

    // Last tile info
    public string SavedGraphicsDir;
    public string TileLastSearched;
    public string TileLastUsed;
    public bool TileUseUnlit;
    public bool TileUseRain;

    // Last prop info
    public string SavedPropsDir;
    public string PropLastSearched;
    public string PropLastUsed;
    public bool PropUseUnlit;
    public bool PropUseRain;

    /// <summary>
    /// Loads the default options
    /// </summary>
    [JsonConstructor]
    public UserData()
    {
        SavedWindowPos = new Vector2Int(20, 20);
        SavedWindowSize = new Vector2Int(500, 500);
        WindowState = WindowState.Normal;
        ScalingFactor = Vector2Int.One;
        VSync = false;
        DebugGraphics = false;

        SavedPaletteDir = "";
        UsingPalette1 = "";
        UsingPalette2 = "";
        PaletteBlend = 0;

        EffectColorA = 0;
        EffectColorB = 3;

        SavedGraphicsDir = "";
        TileLastSearched = "";
        TileLastUsed = "";
        TileUseUnlit = false;
        TileUseRain = false;

        SavedPropsDir = "";
        PropLastSearched = "";
        PropLastUsed = "";
        PropUseUnlit = false;
        PropUseRain = false;
    }

    /// <summary>
    /// Loads from given managers
    /// </summary>
    public UserData(RWObjectManager objects, PaletteManager palettes)
    {
        SavedWindowPos = new Vector2Int(GuiManager.WindowPosX, GuiManager.WindowPosY);
        SavedWindowSize = new Vector2Int(GuiManager.WindowWidth, GuiManager.WindowHeight);
        WindowState = GuiManager.Window.WindowState;
        ScalingFactor = (Vector2Int)GuiManager.ScaleFactor;
        VSync = GuiManager.GraphicsDevice.SyncToVerticalBlank;
        DebugGraphics = GuiManager.DebugGraphics;

        SavedPaletteDir = palettes.PaletteDir;
        UsingPalette1 = palettes.PaletteA.Name;
        UsingPalette2 = palettes.PaletteB.Name;
        PaletteBlend = palettes.PaletteBlend;

        EffectColorA = palettes.EffectColorA;
        EffectColorB = palettes.EffectColorB;

        SavedGraphicsDir = objects.GraphicsDir;
        TileLastSearched = objects.TileLastSearched;
        TileLastUsed = objects.TileLastUsed;
        TileUseUnlit = objects.TileUseRain;
        TileUseRain = objects.PropUseRain;

        SavedPropsDir = objects.PropsDir;
        PropLastSearched = objects.PropLastSearched;
        PropLastUsed = objects.PropLastUsed;
        PropUseUnlit = objects.PropUseUnlit;
        PropUseRain = objects.PropUseRain;
    }

    public static UserData Load(string path)
    {
        UserData? loaded = JsonSerializer.Deserialize<UserData>(
            File.ReadAllText(path),
            JsonOptions
        );

        if (loaded == null)
        {
            throw new JsonException($"An error occured while parsing userdata!\nPlease check {path}");
        }

        return loaded;
    }

    public void Save(string path)
    {
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                this,
                JsonOptions
            )
        );
    }
}