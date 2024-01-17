using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RWBaker.GeneralTools;

public static class PaletteManager
{
    public static Palette PaletteA { get; private set;  }
    public static Palette PaletteB { get; private set;  }
    public static Palette CurrentPalette { get; private set;  }
    public static readonly List<Palette> Palettes = new();

    public static bool CurrentChanged { get; private set; }

    private static int _changedResetCounter;

    public static void Update()
    {
        if (!CurrentChanged) return;
        
        _changedResetCounter++;

        if (_changedResetCounter >= 2) CurrentChanged = false;
    }
    
    public static void Load(Context context)
    {
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
    
    public static void PaletteCheck(Context context)
    {
        try
        {
            Palette default0 = default;
            Palette A;
            Palette B;
            
            if (context.UsingPalette1 == "" || context.UsingPalette2 == "")
            {
                default0 = new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "DEFAULT_0");
            }
            
            if (context.UsingPalette1 == "" && context.UsingPalette2 == "")
            {
                PaletteA = default0;
                PaletteB = default0;
                CurrentPalette = default0;
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

            PaletteA = A;
            PaletteB = B;

            if (context.Palette1Percentage == 100)
            {
                CurrentPalette = A;
                return;
            }
            
            if (context.Palette2Percentage == 100)
            {
                CurrentPalette = B;
                return;
            }
            
            CurrentPalette = Palette.MixPalettes(A, B, context.Palette1Percentage, context.Palette2Percentage);
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERROR WHILE LOADING PALETTES ON STARTUP:\n{e}");
        }
    }
    
    public static void MixIntoCurrent(Palette palA, Palette palB, int aPercent, int bPercent)
    {
        CurrentPalette = Palette.MixPalettes(palA, palB, aPercent, bPercent);
        CurrentChanged = true;
    }

    public static void SetA(Palette a)
    {
        PaletteA = a;
    }
    
    public static void SetB(Palette b)
    {
        PaletteB = b;
    }

    public static void Unload()
    {
        foreach (Palette palette in Palettes)
        {
            palette.Dispose();
        }
        
        Palettes.Clear();
        PaletteA.Dispose();
        PaletteB.Dispose();
        CurrentPalette.Dispose();
    }
}