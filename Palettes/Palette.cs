using RWBaker.Gui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.Palettes;

public class Palette
{
    public readonly string Name;
    public readonly GuiTexture DisplayTex;
    public readonly bool isMixed;

    public readonly RgbaFloat CameraSky;
    public readonly RgbaFloat CameraSkyRain;

    public Palette(Image<Rgba32> image, string name)
    {
        Name = name;

        Texture dTex = GuiManager.TextureFromImage(image);
        DisplayTex = GuiTexture.Create(name, dTex);
        DisplayTex.Use();

        isMixed = false;

        CameraSky = image[0, 0].ToRgbaFloat();
        CameraSkyRain = image[0, 8].ToRgbaFloat();
    }

    private Palette(Palette palette1, Palette palette2, float blend)
    {
        Name = $"{palette1.Name} {blend} {palette2.Name}";

        using Image<Rgba32> palAImage = palette1.DisplayTex.ToImage(true);
        using Image<Rgba32> palBImage = palette2.DisplayTex.ToImage(true);

        using Image<Rgba32> mixed = ImageUtils.ImageMix(palAImage, palBImage, new Vector2Int(32, 16), (100f - blend) / 100f);

        Texture dTex = GuiManager.TextureFromImage(mixed);
        DisplayTex = GuiTexture.Create(Name, dTex);
        DisplayTex.Use();

        isMixed = true;
    }

    public static Palette MixPalettes(Palette palA, Palette palB, int blend)
    {
        return blend switch
        {
            0 => palA,
            100 => palB,
            _ => new Palette(palA, palB, blend)
        };
    }

    public static Palette MixPalettes(Palette palA, Palette palB, float blend) => new(palA, palB, blend);

    public void Release()
    {
        DisplayTex.Release();
    }
}