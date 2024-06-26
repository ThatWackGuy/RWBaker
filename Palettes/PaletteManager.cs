using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RWBaker.Gui;
using RWBaker.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

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

    public readonly DeviceBuffer PaletteInfo;

    public delegate void PalettesChangedEvent();
    public event PalettesChangedEvent PalettesChanged = () => { };

    /// <remarks>
    ///  Before creating a new manager consider Program.Palettes
    /// </remarks>
    public PaletteManager(UserData userData)
    {
        PaletteDir = userData.SavedPaletteDir;
        PaletteBlend = userData.PaletteBlend;

        _defaultEffectColors = GuiTexture.CreateFromImage("_effectColors", Utils.GetEmbeddedBytes("res.effectcolors.png"));
        EffectColors = _defaultEffectColors;
        _effectColorA = userData.EffectColorA;
        _effectColorB = userData.EffectColorB;

        _defaultPalette = new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "default0");
        _paletteA = _defaultPalette;
        _paletteB = _defaultPalette;
        _currentPalette = _defaultPalette;

        _lastA = userData.UsingPalette1;
        _lastB = userData.UsingPalette2;

        PaletteInfo = GuiManager.ResourceFactory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        PalettesChanged += () =>
        {
            _lastA = _paletteA.Name;
            _lastB = _paletteB.Name;

            GuiManager.GraphicsDevice.UpdateBuffer(PaletteInfo, 0, new PaletteUniform(this));
        };
    }

    public void GetPalettes(string path)
    {
        foreach (Palette palette in Palettes)
        {
            palette.Release();
        }

        Palettes.Clear();

        if (EffectColors != _defaultEffectColors) EffectColors?.Release();

        if (_currentPalette != _defaultPalette) _currentPalette.Release();

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

                using Image<Rgba32> image = Image.Load<Rgba32>(file);
                Palette palette = new(image, fileName);

                Palettes.Add(palette);
            }
        }

        PaletteDir = path;

        EffectColors ??= _defaultEffectColors;

        if (Palettes.Count == 0)
        {
            _paletteA = _defaultPalette;
            _paletteB = _defaultPalette;
            _currentPalette = _defaultPalette;

            PalettesChanged();
            return;
        }

        _paletteA = Palettes.Any(p => p.Name == _lastA) ? Palettes.First(p => p.Name == _lastA) : Palettes.First();
        _paletteB = Palettes.Any(p => p.Name == _lastB) ? Palettes.First(p => p.Name == _lastB) : Palettes.First();

        _currentPalette = Palette.MixPalettes(PaletteA, PaletteB, PaletteBlend);
        _lastA = PaletteA.Name;
        _lastB = PaletteB.Name;

        PalettesChanged();
    }

    public void Dispose()
    {
        foreach (Palette palette in Palettes)
        {
            palette.Release();
        }

        Palettes.Clear();

        _defaultPalette.Release();

        _defaultEffectColors.Dispose();
        EffectColors?.Dispose();

        GC.SuppressFinalize(this);
    }
}