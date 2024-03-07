using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using SixLabors.ImageSharp;
using Veldrid;

namespace RWBaker.Props;

public class SoftProp : IProp
{
    // default fields
    private bool _hasWarnings;
    private string _warnings;

    private readonly string _category;
    private readonly Vector3 _categoryColor;

    private readonly string _name;
    private readonly string _properName;
    private readonly string _searchName;

    private readonly PropTag _tags;

    private readonly int _variations;
    private readonly bool _random;

    private readonly string[] _notes;

    // soft prop fields
    private readonly Vector2Int _size;
    private readonly int _depth;
    private readonly int[] _repeatLayers;
    private readonly bool _colorize;
    private readonly int _smoothShading;
    private readonly float _contourExponent;
    private readonly float _highlightMin;
    private readonly float _shadowMin;
    private readonly float _highlightExponent;

    public SoftProp(RWObjectManager manager, PropType type, string line, string category, Vector3 categoryColor)
    {
        _hasWarnings = false;
        _warnings = "";

        _category = category;
        _categoryColor = categoryColor;

        _name = Regex.Match(line, "#nm *: *\"(.*?)\"").Groups[1].Value;
        _properName = $"{_category} - {_name}";

        string filePath = Path.Combine(manager.PropsDir, $"{_name}.png");
        bool fileExists = true;
        if (!File.Exists(filePath))
        {
            fileExists = false;
            LogWarning("The image for the prop couldn't be found. Please check the names or if the file has been deleted.");
        }

        _size = Vector2Int.One;
        switch (type)
        {
            // unvaried soft props have implicit sizes
            case PropType.Soft:
            {
                if (!fileExists) break;

                using Image propImg = Image.Load(filePath);
                _size.X = propImg.Size.Width;
                _size.Y = propImg.Size.Height;

                break;
            }

            case PropType.VariedSoft:
            {
                Match sizeStr = Regex.Match(line, @"#pxlSize *: *point\( *([0-9]+) *, *([0-9]+) *\)");
                if (!RWUtils.LingoInt(sizeStr.Groups[1].Value, out _size.X))
                {
                    _size.X = 1;
                    LogWarning("Couldn't parse Size X component");
                }
                if (!RWUtils.LingoInt(sizeStr.Groups[2].Value, out _size.Y))
                {
                    _size.Y = 1;
                    LogWarning("Couldn't parse Size Y component");
                }

                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        // DEPTH
        string depthStr = Regex.Match(line, "#depth *: *([0-9]*)").Groups[1].Value;
        if (!RWUtils.LingoInt(depthStr, out _depth))
        {
            LogWarning("Prop was Soft but depth couldn't be parsed. Defaulting to 1.");

            _depth = 1;
        }

        // VARIATIONS
        string variationsStr = Regex.Match(line, "#vars *: *([0-9]*)").Groups[1].Value;
        if (!RWUtils.LingoInt(variationsStr, out _variations))
        {
            if (type == PropType.VariedSoft)
            {
                LogWarning("Prop was VariedSoft but variation couldn't be parsed. Defaulting to 1.");
            }

            _variations = 1;
        }

        // there is no such thing as 0 variation, smh
        if (_variations < 1)
        {
            LogWarning("Vars was less than 1! Defaulting back to 0");
            _variations = 1;
        }

        // COLORIZE
        string colorizeStr = Regex.Match(line, "#colorize *: *([0-1]){1}").Groups[1].Value;
        if (!RWUtils.LingoBool(colorizeStr, out _colorize))
        {
            if (type == PropType.VariedSoft)
            {
                LogWarning("Prop was VariedSoft but random couldn't be parsed. Defaulting to false.");
            }

            _colorize = false;
        }

        // SMOOTH SHADING
        string smoothShadeStr = Regex.Match(line, "#smoothShading *: *([0-9]*)").Groups[1].Value;
        if (!RWUtils.LingoInt(smoothShadeStr, out _smoothShading))
        {
            LogWarning("Couldn't parse smooth shading. Defaulting to 1.");
            _smoothShading = 1;
        }

        // CONTOUR EXP
        string contourExpStr = Regex.Match(line, "#contourExp *: *([0-9]+\\.?(?:[0-9]+)?)*").Groups[1].Value;
        if (!RWUtils.LingoFloat(contourExpStr, out _contourExponent))
        {
            LogWarning("Couldn't parse contour exponent (contourExp). Defaulting to 1.");
            _contourExponent = 1;
        }

        // HIGHLIGHT BORDER
        string highlightBorderStr = Regex.Match(line, "#highLightBorder *: *([0-9]+\\.?(?:[0-9]+)?)*").Groups[1].Value;
        if (!RWUtils.LingoFloat(highlightBorderStr, out _highlightMin))
        {
            LogWarning("Couldn't parse highlight minimum (highlightBorder). Defaulting to 1.");
            _highlightMin = 1;
        }

        // SHADOW BORDER
        string shadowBorderStr = Regex.Match(line, "#shadowBorder *: *([0-9]+\\.?(?:[0-9]+)?)*").Groups[1].Value;
        if (!RWUtils.LingoFloat(shadowBorderStr, out _shadowMin))
        {
            LogWarning("Couldn't parse shadow minimum (shadowBorder). Defaulting to 1.");
            _shadowMin = 1;
        }

        // DEPTH AFFECT HILITES
        string depthAffectStr = Regex.Match(line, "#depthAffectHilites *: *([0-9]+\\.?(?:[0-9]+)?)*").Groups[1].Value;
        if (!RWUtils.LingoFloat(depthAffectStr, out _highlightExponent))
        {
            LogWarning("Couldn't parse highlight exponent (depthAffectHilites). Defaulting to 1.");
            _highlightExponent = 1;
        }

        // RANDOM
        string randomStr = Regex.Match(line, "#random *: *([0-1]){1}").Groups[1].Value;
        if (!RWUtils.LingoBool(randomStr, out _random))
        {
            if (type == PropType.VariedSoft)
            {
                LogWarning("Prop was VariedSoft but random couldn't be parsed. Defaulting to false.");
            }

            _random = false;
        }

        // TAGS
        string tagsRaw = Regex.Match(line, @"#tags *: *\[(.*?)\]").Groups[1].Value;
        string[] tagsList = Regex.Matches(tagsRaw, "\"(.*?)\"").Select(m => m.Groups[1].Value).ToArray();

        foreach (string t in tagsList)
        {
            if (!RWUtils.LingoEnum(t.Replace(" ", ""), PropTag.None, out PropTag tag))
            {
                LogWarning($"Couldn't parse prop tag '{t}'.");
            }

            _tags |= tag;
        }

        _searchName = $"{_category} {_name} {type} {string.Join(' ', _tags)} {_size.X}x{_size.Y}".ToLower();

        // soft props only repeat once
        _repeatLayers = new int[_depth];
        Array.Fill(_repeatLayers, 1);

        if (_hasWarnings)
        {
            manager.PropLoadLogs += _properName + ":\n" + _warnings + "\n";
        }
    }

    public Vector3 CategoryColor() => _categoryColor;

    public string Name() => _name;
    public string ProperName() => _properName;
    public string SearchName() => _searchName;

    public bool HasWarnings() => _hasWarnings;
    public string Warnings() => _warnings;

    public IProp.UniformConstructor GetUniform() => cached =>
    {
        DeviceBuffer buffer = GuiManager.ResourceFactory.CreateStructBuffer<RWSoftPropRenderUniform>();
        GuiManager.GraphicsDevice.UpdateBuffer(
            buffer,
            0,
            new RWSoftPropRenderUniform(
                cached,
                (Vector2)_size,
                _variations,
                _colorize /*flag check to see if it is colored*/,
                _smoothShading,
                _contourExponent,
                _highlightMin,
                _shadowMin,
                _highlightExponent
            )
        );

        return buffer;
    };

    public IProp.TexPosCalculator GetTexPos() => (var, _) => new Vector2(_size.X * var,  1);
    public ShaderSetDescription ShaderSetDescription() => RWUtils.SoftPropRendererShaderSet;

    public int Variants() => _variations;

    public Vector2Int Size() => _size;

    public int[] RepeatLayers() => _repeatLayers;

    public void LogWarning(string warn)
    {
        _hasWarnings = true;
        _warnings += "\t" + warn  + "\n";
    }
}