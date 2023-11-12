using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWBaker.GraphicsTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.GeneralTools;

public struct Palette : IDisposable
{
    public static Palette A;
    public static Palette B;
    public static Palette Current;
    
    public static readonly List<Palette> Palettes = new();

    public string Name;
    public readonly Image<Rgba32> ColorsImage;
    public readonly GuiTexture DisplayTex;
    public readonly bool isMixed;

    public Palette(Image<Rgba32> image, string name)
    {
        Name = name;
        ColorsImage = image;

        Texture dTex = Graphics.TextureFromImage(image);
        DisplayTex = GuiTexture.Create(name, dTex);
        isMixed = false;
    }
    
    private Palette(Palette palette1, int palette1Percent, Palette palette2, int palette2Percent)
    {
        Name = $"{palette1Percent} {palette1.Name} + {palette2Percent} {palette2.Name}";
        ColorsImage = ImageUtils.ImageMix(palette1.ColorsImage, palette2.ColorsImage, new Vector2Int(32, 16), palette1Percent / 100f, palette2Percent / 100f);
        
        Texture dTex = Graphics.TextureFromImage(ColorsImage);
        DisplayTex = GuiTexture.Create(Name, dTex);
        isMixed = true;
    }

    public static void GetPalettes()
    {
        Context context = Context.GetContext();
        
        if (context.SavedPaletteDir == "") return;
    
        foreach (Palette palette in Palettes)
        {
            palette.DisplayTex.Dispose();
        }
        
        Palettes.Clear();

        foreach (string file in Directory.EnumerateFiles(context.SavedPaletteDir))
        {
            if (Path.GetExtension(file) != ".png") continue;
            
            string fileName = Path.GetFileNameWithoutExtension(file);
            
            if (!fileName.StartsWith("palette")) continue;

            Image<Rgba32> image = Image.Load<Rgba32>(file);
            Palette palette = new(image, fileName);
            
            Palettes.Add(palette);
        }
    }

    public static void Load()
    {
        GetPalettes();
        
        Context context = Context.GetContext();
        
        try
        {
            Palette default0 = default;
            
            if (context.UsingPalette1 == "" || context.UsingPalette2 == "")
            {
                default0 = new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "DEFAULT_0");
            }
            
            if (context.UsingPalette1 == "" && context.UsingPalette2 == "")
            {
                A = default0;
                B = default0;
                Current = default0;
                return;
            }

            if (context.UsingPalette1 == "")
            {
                A = default0;
            }
            else
            {
                A = Palettes.Any(p => p.Name == context.UsingPalette1) 
                    ? Palettes.First(p => p.Name == context.UsingPalette1)
                    : new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "UNKNOWN_0");
            }

            if (context.UsingPalette2 == "")
            {
                B = default0;
            }
            else
            {
                B = Palettes.Any(p => p.Name == context.UsingPalette2)
                    ? Palettes.First(p => p.Name == context.UsingPalette2)
                    : new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "UNKNOWN_0");
            }

            if (context.Palette1Percentage == 100)
            {
                Current = A;
                return;
            }
            
            if (context.Palette2Percentage == 100)
            {
                Current = B;
                return;
            }

            Current = new Palette(A, context.Palette1Percentage, B, context.Palette2Percentage);
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERROR WHILE LOADING PALETTES ON STARTUP:\n{e}");
        }
    }

    public static Palette MixPalettes(Palette palA, Palette palB, int aPercent, int bPercent)
    {
        if (aPercent == 100) return palA;
        if (bPercent == 100) return palB;
        if (Current.Name == $"{aPercent} {palA.Name} + {bPercent} {palB.Name}") return Current;

        if (Current.isMixed) Current.DisplayTex.Dispose();

        return new Palette(palA, aPercent, palB, bPercent);
    }

    public void Dispose()
    {
        ColorsImage.Dispose();
        DisplayTex.Dispose();
    }
}