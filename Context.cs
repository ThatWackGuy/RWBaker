using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Veldrid;

namespace RWBaker;

/// <summary>
/// General info on the current instance of the app
/// <br/> <br/>
/// Loads data/userdata.json on startup, saves it back on close
/// </summary>
[Serializable]
public class Context
{
    private static JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = false,
        IncludeFields = true,
        WriteIndented = true
    };
    
    // Window options
    public Vector2Int SavedWindowPos;
    public Vector2Int SavedWindowSize;
    public WindowState WindowState;
    public bool VSync;
    public bool GraphicsDebug;
    
    // Paths
    public string SavedGraphicsDir;
    public string SavedPropsDir;
    public string SavedPaletteDir;
    
    // Last palette names
    public string UsingPalette1;
    public string UsingPalette2;

    // Last palette percentages
    public int Palette1Percentage;
    public int Palette2Percentage;

    // Last tile data
    public string TileLastSearched;
    public string TileLastUsed;
    public bool TileUseUnlit;
    public bool TileUseRain;
    public bool TileOutputToFile;

    /// <summary>
    /// Loads the default options
    /// </summary>
    /// <remarks>
    ///  Before creating a new context consider Program.<see cref="Program.Context"/>
    /// </remarks>
    [JsonConstructor]
    public Context()
    {
        SavedWindowPos = new Vector2Int(20, 20);
        SavedWindowSize = new Vector2Int(500, 500);
        WindowState = WindowState.Normal;
        VSync = false;
        GraphicsDebug = false;
        
        SavedGraphicsDir = "";
        SavedPropsDir = "";
        SavedPaletteDir = "";
        
        UsingPalette1 = "";
        UsingPalette2 = "";
        
        Palette1Percentage = 100;
        Palette2Percentage = 0;
        
        TileLastSearched = "";
        TileLastUsed = "";
        TileUseUnlit = false;
        TileUseRain = false;
        TileOutputToFile = false;
    }

    /// <summary>
    /// returns a new context from the given json file path
    /// </summary>
    /// <remarks>
    ///  Before creating a new context consider Program.<see cref="Program.Context"/>
    /// </remarks>
    public static Context Load(string path)
    {
        Context? loaded = JsonSerializer.Deserialize<Context>(
            File.ReadAllText(path),
            JsonOptions
        );

        if (loaded is null)
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