using System;
using RWBaker.Gui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.Palettes;

public class Palette : IDisposable
{
    public readonly string Name;
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

    private Palette(Palette palette1, Palette palette2, float blend)
    {
        Name = $"{palette1.Name} {blend} {palette2.Name}";
        Image = ImageUtils.ImageMix(palette1.Image, palette2.Image, new Vector2Int(32, 16), (100f - blend) / 100f);

        Texture dTex = GuiManager.TextureFromImage(Image);
        DisplayTex = GuiTexture.Create(Name, dTex);
        isMixed = true;
    }

    public static Palette MixPalettes(Palette palA, Palette palB, int blend)
    {
        if (blend == 0) return palA;
        if (blend == 100) return palB;

        return new Palette(palA, palB, blend);
    }

    public static Palette MixPalettes(Palette palA, Palette palB, float blend) => new(palA, palB, blend);

    public void Dispose()
    {
        Image.Dispose();
        DisplayTex.Dispose();
    }
}