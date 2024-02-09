using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWBaker.Gui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RWBaker.Palettes;

public class PaletteManager : IDisposable
{
    private Palette _defaultPalette;
    public readonly List<Palette> Palettes = new();

    public string PaletteDir;
    public int PaletteBlend;

    private Palette _paletteA;
    public Palette PaletteA
    {
        get => _paletteA;

        set
        {
            _paletteA = value;
            PalettesChanged();
        }
    }

    private Palette _paletteB;
    public Palette PaletteB
    {
        get => _paletteB;

        set
        {
            _paletteB = value;
            PalettesChanged();
        }
    }

    private Palette _currentPalette;
    public Palette CurrentPalette
    {
        get => _currentPalette;

        set
        {
            _currentPalette = value;
            PalettesChanged();
        }
    }

    private string _lastA;
    private string _lastB;

    private GuiTexture _defaultEffectColors;
    public GuiTexture EffectColors { get; private set; }

    private int _effectColorA;
    public int EffectColorA
    {
        get => _effectColorA;

        set
        {
            _effectColorA = value;
            PalettesChanged();
        }
    }

    private int _effectColorB;
    public int EffectColorB
    {
        get => _effectColorB;

        set
        {
            _effectColorB = value;
            PalettesChanged();
        }
    }

    public delegate void PalettesChangedEvent();
    public event PalettesChangedEvent PalettesChanged = () => { };

    /// <remarks>
    ///  Before creating a new manager consider Context.Palettes
    /// </remarks>
    public PaletteManager(UserData userData)
    {
        PaletteDir = userData.SavedPaletteDir;
        PaletteBlend = userData.PaletteBlend;

        _defaultEffectColors = GuiTexture.CreateFromImage("_effectColors", Utils.GetEmbeddedBytes("res.effectcolors.png"));
        EffectColors = _defaultEffectColors;
        _effectColorB = userData.EffectColorA;
        _effectColorB = userData.EffectColorB;

        _defaultPalette = new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "default0");
        _paletteA = _defaultPalette;
        _paletteB = _defaultPalette;
        _currentPalette = _defaultPalette;

        _lastA = userData.UsingPalette1;
        _lastB = userData.UsingPalette2;
    }

    public void GetPalettes(string path)
    {
        foreach (Palette palette in Palettes)
        {
            palette.Dispose();
        }
        Palettes.Clear();

        if (EffectColors != _defaultEffectColors)EffectColors?.Dispose();

        if (_paletteA != _defaultPalette) _paletteA.Dispose();
        if (_paletteB != _defaultPalette) _paletteB.Dispose();
        if (_currentPalette != _defaultPalette) _currentPalette.Dispose();

        if (path != "")
        {
            foreach (string file in Directory.EnumerateFiles(path))
            {
                if (Path.GetExtension(file) != ".png") continue;

                string fileName = Path.GetFileNameWithoutExtension(file);

                if (!fileName.StartsWith("palette")) continue;

                if (fileName.StartsWith("effectcolors"))
                {
                    EffectColors = GuiTexture.CreateFromImage("_effectColors", file);
                    continue;
                }

                Image<Rgba32> image = Image.Load<Rgba32>(file);
                Palette palette = new(image, fileName);

                Palettes.Add(palette);
            }
        }

        PaletteDir = path;

        if (EffectColors == null)
        {
            EffectColors = _defaultEffectColors;
        }

        if (Palettes.Count == 0)
        {
            _paletteA = _defaultPalette;
            _paletteB = _defaultPalette;
            _currentPalette = _defaultPalette;

            PalettesChanged();
            return;
        }

        if (_lastA != "")
        {
            if (Palettes.Any(p => p.Name == _lastA))
            {
                _paletteA = Palettes.First(p => p.Name == _lastA);
            }
            else
            {
                _paletteA = Palettes.First();
            }
        }

        if (_lastB != "")
        {
            if (Palettes.Any(p => p.Name == _lastB))
            {
                _paletteB = Palettes.First(p => p.Name == _lastB);
            }
            else
            {
                _paletteB = Palettes.First();
            }
        }

        _currentPalette = Palette.MixPalettes(PaletteA, PaletteB, PaletteBlend);
        _lastA = PaletteA.Name;
        _lastB = PaletteB.Name;

        PalettesChanged();
    }

    public void Dispose()
    {
        foreach (Palette palette in Palettes)
        {
            palette.Dispose();
        }
        Palettes.Clear();

        _defaultPalette.Dispose();

        _defaultEffectColors.Dispose();
        EffectColors?.Dispose();

        GC.SuppressFinalize(this);
    }
}