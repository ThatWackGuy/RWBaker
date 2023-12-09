using System;
using RWBaker.GraphicsTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.GeneralTools;

public struct Palette : IDisposable
{
    public string Name;
    public readonly Image<Rgba32> Image;
    public readonly GuiTexture DisplayTex;
    public readonly bool isMixed;

    public Palette(Image<Rgba32> image, string name)
    {
        Name = name;
        Image = image;

        Texture dTex = GuiManager.TextureFromImage(image);
        DisplayTex = GuiTexture.Create(name, dTex);
        isMixed = false;
    }
    
    private Palette(Palette palette1, int palette1Percent, Palette palette2, int palette2Percent)
    {
        Name = $"{palette1Percent} {palette1.Name} + {palette2Percent} {palette2.Name}";
        Image = ImageUtils.ImageMix(palette1.Image, palette2.Image, new Vector2Int(32, 16), palette1Percent / 100f, palette2Percent / 100f);
        
        Texture dTex = GuiManager.TextureFromImage(Image);
        DisplayTex = GuiTexture.Create(Name, dTex);
        isMixed = true;
    }

    public static Palette MixPalettes(Palette palA, Palette palB, int aPercent, int bPercent)
    {
        if (aPercent == 100) return palA;
        if (bPercent == 100) return palB;
        if (Program.CurrentPalette.Name == $"{aPercent} {palA.Name} + {bPercent} {palB.Name}") return Program.CurrentPalette;

        if (Program.CurrentPalette.isMixed) Program.CurrentPalette.DisplayTex.Dispose();

        return new Palette(palA, aPercent, palB, bPercent);
    }

    public void Dispose()
    {
        Image.Dispose();
        DisplayTex.Dispose();
    }
}