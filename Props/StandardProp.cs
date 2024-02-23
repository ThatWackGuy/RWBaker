using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Props;

public enum PropColorTreatment
{
    Standard,
    Bevel
}

public class StandardProp : IProp
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

    private readonly string[] _notes;

    // standard prop fields
    private readonly Vector2Int _size;
    private readonly int[] _repeatLayers;

    private readonly PropColorTreatment _treatment;
    private readonly int _bevelAmount;

    private readonly bool _colorize; // on varied

    public StandardProp(RWObjectManager manager, PropType type, string line, string category, Vector3 categoryColor)
    {
        _hasWarnings = false;
        _warnings = "";

        _category = category;
        _categoryColor = categoryColor;

        _name = Regex.Match(line, "#nm *: *\"(.*?)\"").Groups[1].Value;
        _properName = $"{_category} - {_name}";

        Match sizeStr = Regex.Match(line, @"#sz *: *point\( *([0-9]+) *, *([0-9]+) *\)");
        if (!RWUtils.LingoInt(sizeStr.Groups[1].Value, out int sizeX))
        {
            sizeX = 1;
            LogWarning("Couldn't parse Size X component");
        }
        if (!RWUtils.LingoInt(sizeStr.Groups[2].Value, out int sizeY))
        {
            sizeY = 1;
            LogWarning("Couldn't parse Size Y component");
        }
        _size = new Vector2Int(sizeX, sizeY);

        // REPEATING LAYERS
        string[] repeatLayers = Regex.Match(line, @"#repeatL *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");

        if (!RWUtils.LingoIntArray(repeatLayers, out _repeatLayers))
        {
            LogWarning($"Couldn't parse repeatL");
        }

        if (_repeatLayers.Length > 30)
        {
            LogWarning($"30+ layers aren't supported! {repeatLayers.Length} layers on prop.");
        }

        if (_repeatLayers.Sum() >= 30)
        {
            LogWarning($"30+ sub-layers aren't supported! {_repeatLayers.Sum()} sub-layers on prop.");
        }

        // COLOR TREATMENT
        string treatmentStr = Regex.Match(line, "#colorTreatment *: *\"(.*?)\"").Groups[1].Value;
        if (!RWUtils.LingoEnum(treatmentStr, PropColorTreatment.Standard, out _treatment))
        {
            LogWarning($"Couldn't parse color treatment type '{treatmentStr}'. Defaulting to Standard.");
        }

        if (_treatment == PropColorTreatment.Bevel)
        {
            string bevelAmountStr = Regex.Match(line, "#bevel *: *([0-9]*)").Groups[1].Value;
            if (!RWUtils.LingoInt(bevelAmountStr, out _bevelAmount))
            {
                LogWarning("Prop had bevel color treatment but had no bevel amount. Defaulting to 1");
                _bevelAmount = 1;
            }
        }
        else
        {
            _bevelAmount = 0;
        }

        // VARIATIONS
        string rnd = Regex.Match(line, "#vars *: *([0-9]*)").Groups[1].Value;
        if (!RWUtils.LingoInt(rnd, out _variations))
        {
            if (type == PropType.VariedStandard)
            {
                LogWarning("Prop was VariedStandard but no variation number was given. Defaulting to 1.");
            }

            _variations = 1;
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

        // COLORIZE
        string colorizeStr = Regex.Match(line, "#colorize *: *([0-1]){1}").Groups[1].Value;
        if (!RWUtils.LingoBool(colorizeStr, out _colorize) && _variations > 1)
        {
            LogWarning("Couldn't parse colorize even though prop was Varied.");
        }

        // SEARCH NAME
        _searchName = $"{_category} {_name} {type} {string.Join(' ', _tags)} {sizeX}x{sizeY}".ToLower();

        if (!File.Exists(manager.PropsDir + "/" + _name + ".png"))
        {
            LogWarning("The image for the prop couldn't be found. Please check the names or if the file has been deleted.");
        }

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
        DeviceBuffer buffer = GuiManager.ResourceFactory.CreateStructBuffer<RWStandardPropRenderUniform>();
        GuiManager.GraphicsDevice.UpdateBuffer(
            buffer,
            0,
            new RWStandardPropRenderUniform(
                (Vector2)_size * 20,
                _variations,
                _bevelAmount,
                (_tags & PropTag.Colored) != 0 /*flag check to see if it is colored*/,
                cached.UseRainPalette
            )
        );

        return buffer;
    };

    public IProp.TexPosCalculator GetTexPos() => (var, layer) => new Vector2(_size.X * 20 * var,  1 + layer * _size.Y * 20);

    public ShaderSetDescription ShaderSetDescription() => RWUtils.StandardPropRendererShaderSet;

    public int Variants() => _variations;

    public Vector2Int Size() => _size * 20;

    public int[] RepeatLayers() => _repeatLayers;

    public void LogWarning(string warn)
    {
        _hasWarnings = true;
        _warnings += "\t" + warn  + "\n";
    }
}