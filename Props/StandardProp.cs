using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.Gui;
using RWBaker.Rendering;
using Veldrid;

namespace RWBaker.Props;

public enum PropColorTreatment
{
    Standard,
    Bevel
}

public class StandardProp : Prop
{
    private readonly PropTag _tags;

    private readonly int _variations;

    private readonly string[] _notes;

    // standard prop fields
    private readonly Vector2 _size;
    private readonly int[] _repeatLayers;

    private readonly PropColorTreatment _treatment;
    private readonly int _bevelAmount;

    private readonly bool _colorize; // on varied

    public StandardProp(RWObjectManager manager, PropType type, string line, string category, Vector3 categoryColor)
    {
        Category = category;
        CategoryColor = categoryColor;

        Name = Regex.Match(line, "#nm *: *\"(.*?)\"").Groups[1].Value;
        ProperName = $"{Category} - {Name}";

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
        _size = new Vector2(sizeX, sizeY);

        // REPEATING LAYERS
        string[] repeatLayers = Regex.Match(line, @"#repeatL *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");

        if (!RWUtils.LingoIntArray(repeatLayers, out _repeatLayers))
        {
            LogWarning("Couldn't parse repeatL");
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
        SearchName = $"{Category} {Name} {type} {string.Join(' ', _tags)} {sizeX}x{sizeY}".ToLower();

        if (!File.Exists(manager.PropsDir + "/" + Name + ".png"))
        {
            LogWarning("The image for the prop couldn't be found. Please check the names or if the file has been deleted.");
        }

        if (HasWarnings)
        {
            manager.PropLoadLogs += ProperName + ":\n" + Warnings + "\n";
        }
    }

    public override PropObject AsObject(Scene scene, RWObjectManager objectManager) => new(
        Name,
        ProperName,
        CompleteRenderDescription,
        GetTexPos,
        _size * 20,
        _repeatLayers,
        _variations,
        scene,
        objectManager
    );

    private RenderDescription CompleteRenderDescription(RWVertexData[] vertices, ushort[] indices, PropObject instance, Camera camera, Texture texture) => new(
        "standard_prop",
        vertices,
        indices,
        GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectTextureLayout,
                texture,
                camera.Scene.PaletteManager.CurrentPalette.DisplayTex.Texture,
                camera.Scene.PaletteManager.EffectColors.Texture,
                camera.LightingPass.RenderTexture.Texture,
                camera.RemovalPass.RenderTexture.Texture
            )
        ),
        new RWStandardPropRenderUniform(
            instance,
            _size * 20,
            _variations,
            _bevelAmount,
            (_tags & PropTag.Colored) != 0 /*flag check to see if it is colored*/
        ),
        false, true, true,
        RWUtils.RWResourceLayout, RWUtils.StandardPropShaders,
        [], []
    );

    private Vector2 GetTexPos(int var, int layer) => new(_size.X * 20 * var,  1 + layer * _size.Y * 20);
}