using System;
using System.Collections.Generic;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;
using RWBaker.PropTools;
using RWBaker.TileTools;
using SixLabors.ImageSharp;
using Veldrid;

namespace RWBaker;

public static class Program
{
    public static GuiTexture IconTexture;
    
    public static readonly List<Tile> Tiles = new();
    public static readonly List<Prop> Props = new();
    
    public static Palette PaletteA;
    public static Palette PaletteB;
    public static Palette CurrentPalette;
    public static readonly List<Palette> Palettes = new();

    public static void Main()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;
        
        Context.LoadContext();
        Context context = Context.GetContext();
        
        GuiManager.Load(context);

        // Load Icon Texture
        Texture iconTex = GuiManager.TextureFromResource("res.bakertex.png");
        IconTexture = GuiTexture.Create("_icon", iconTex);

        if (context.SavedGraphicsDir != "")
        {
            RWUtils.GetTiles(out string tileLog);
            Console.WriteLine($"TILE LOAD LOG:\n{tileLog}\n\n");
        }

        if (context.SavedPaletteDir != "")
        {
            RWUtils.GetPalettes();
            RWUtils.PaletteCheck();
        }
        
        RWUtils.LoadGraphicsResources();
        
        GuiManager.RenderProcess();
    }
}