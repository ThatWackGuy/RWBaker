using System;
using System.IO;
using System.Text.Json;
using Veldrid;

namespace RWBaker;

/// <summary>
/// General info on the current instance of the app
/// <br/> <br/>
/// Loads data/userdata.json on startup, saves it back on close
/// </summary>
[Serializable]
public class Context // Stripped down version of the full context class :3
{
    private static Context? instance;

    // Serialized
    public Vector2Int SavedWindowPos;
    public Vector2Int SavedWindowSize;
    public WindowState WindowState;
    
    public string SavedGraphicsDir = null!;
    public string SavedPropsDir = null!;
    public string SavedPaletteDir = null!;
    
    public string UsingPalette1 = null!;
    public string UsingPalette2 = null!;

    public int Palette1Percentage;
    public int Palette2Percentage;

    public string TileLastSearched = null!;
    public string TileLastUsed = null!;
    public bool TileUseUnlit;
    public bool TileUseRain;
    public bool TileOutputToFile;
    
    public static Context GetContext()
    {
        if (instance is null)
        {
            throw new NullReferenceException("Context is not instanced!");
        }

        return instance;
    }

    private static Context LoadDefaultContext()
    {
        return new Context
        {
            SavedWindowPos = new Vector2Int(20, 20),
            SavedWindowSize = new Vector2Int(500, 500),
            WindowState = WindowState.Normal,
            SavedGraphicsDir = "",
            SavedPropsDir = "",
            SavedPaletteDir = "",
            UsingPalette1 = "DEFAULT",
            UsingPalette2 = "DEFAULT",
            Palette1Percentage = 100,
            Palette2Percentage = 0,
            TileLastSearched = "",
            TileLastUsed = "",
            TileUseUnlit = false,
            TileUseRain = false,
            TileOutputToFile = false
        };
    }

    public static void LoadContext()
    {
        // Create the userdata file if it doesn't exist
        if (!File.Exists("./userdata.json")) File.Create("./userdata.json").Close();

        bool failedLoading = false;
        Exception? exception = null;
        
        try
        {
            Context? loadedContext = JsonSerializer.Deserialize<Context>(
                File.ReadAllText("./userdata.json"),
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = false,
                    IncludeFields = true,
                    WriteIndented = true
                }
            );

            if (loadedContext is null)
            {
                failedLoading = true;
            }
            else
            {
                instance = loadedContext;
            }
        }
        catch (Exception e)
        {
            failedLoading = true;
            exception = e;
        }

        if (failedLoading)
        {
            instance = LoadDefaultContext();
            Console.WriteLine(
                exception is null
                ? "An error occured while parsing userdata"
                : $"An error occured while parsing userdata:\n\t{exception}"
            ); 
        }

        SaveContext();
    }
    
    public static void SaveContext()
    {
        if (instance is null)
        {
            throw new NullReferenceException("Context is not instanced!");
        }
        
        File.WriteAllText(
            "./userdata.json",
            JsonSerializer.Serialize(
                instance,
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = false,
                    IncludeFields = true,
                    WriteIndented = true
                }
            )
        );
    }
}