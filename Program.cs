using System;
using System.Collections.Generic;
using System.IO;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;
using RWBaker.PropTools;
using RWBaker.TileTools;
using SixLabors.ImageSharp;
using Veldrid;

namespace RWBaker;

public static class Program
{
    public static Context Context;
    public static GuiTexture IconTexture;
    
    public static readonly List<Tile> Tiles = new();
    public static readonly List<Prop> Props = new();

    public static void Main()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;

        Exception? contextFailed = null;
        // Create the userdata file if it doesn't exist
        if (!File.Exists("./userdata.json"))
        {
            File.Create("./userdata.json").Close();
            
            Context = new Context();
        }
        else
        {
            try
            {
                Context = Context.Load("./userdata.json");
            }
            catch (Exception e)
            {
                contextFailed = e;
                Context = new Context();
            }
        }

        Context.Save("./userdata.json");

        GuiManager.Load(Context);
        if (contextFailed != null)
        {
            GuiManager.Exception(contextFailed);
        }

        // Load mandatory textures
        Texture iconTex = GuiManager.TextureFromResource("res.bakertex.png");
        IconTexture = GuiTexture.Create("_icon", iconTex);

        if (Context.SavedGraphicsDir != "") RWUtils.GetTiles(Context, out string _);

        PaletteManager.Load(Context);

        RWUtils.LoadGraphicsResources();

        GuiManager.RenderProcess();
    }
}