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

public class AntimatterProp : IProp
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

    private readonly string[] _notes;

    // soft prop fields
    private readonly Vector2Int _size;
    private readonly int _depth;
    private readonly int[] _repeatLayers;

    public AntimatterProp(RWObjectManager manager, PropType type, string line, string category, Vector3 categoryColor)
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
        if (fileExists)
        {
            using Image propImg = Image.Load(filePath);
            _size.X = propImg.Size.Width;
            _size.Y = propImg.Size.Height - 1;
        }

        // DEPTH
        string depthStr = Regex.Match(line, "#depth *: *([0-9]*)").Groups[1].Value;
        if (!RWUtils.LingoInt(depthStr, out _depth))
        {
            LogWarning("Prop was Soft but depth couldn't be parsed. Defaulting to 1.");

            _depth = 1;
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
        DeviceBuffer buffer = GuiManager.ResourceFactory.CreateStructBuffer<RWBasicPropRenderUniform>();
        GuiManager.GraphicsDevice.UpdateBuffer(
            buffer,
            0,
            new RWBasicPropRenderUniform(
                (Vector2)_size,
                1,
                cached.UseRainPalette
            )
        );

        return buffer;
    };

    public IProp.TexPosCalculator GetTexPos() => (_, _) => Vector2.UnitY;
    public ShaderSetDescription ShaderSetDescription() => RWUtils.AntimatterPropRendererShaderSet;

    public int Variants() => 1;

    public Vector2Int Size() => _size;

    public int[] RepeatLayers() => _repeatLayers;

    public void LogWarning(string warn)
    {
        _hasWarnings = true;
        _warnings += "\t" + warn  + "\n";
    }
}