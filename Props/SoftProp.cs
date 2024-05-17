using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.Gui;
using RWBaker.Rendering;
using SixLabors.ImageSharp;
using Veldrid;

namespace RWBaker.Props;

public class SoftProp : Prop
{
    private readonly PropTag _tags;

    private readonly int _variations;
    private readonly bool _random;

    private readonly string[] _notes;

    // soft prop fields
    private readonly Vector2 _size;
    private readonly int _depth;
    private readonly int[] _repeatLayers;
    private readonly bool _colorize;
    private readonly bool _round;
    private readonly bool _selfShade;
    private readonly int _smoothShading;
    private readonly float _contourExponent;
    private readonly float _highlightMin;
    private readonly float _shadowMin;
    private readonly float _highlightExponent;

    public SoftProp(RWObjectManager manager, PropType type, string line, string category, Vector3 categoryColor)
    {
        Category = category;
        CategoryColor = categoryColor;

        Name = Regex.Match(line, "#nm *: *\"(.*?)\"").Groups[1].Value;
        ProperName = $"{Category} - {Name}";

        string filePath = Path.Combine(manager.PropsDir, $"{Name}.png");
        bool fileExists = true;
        if (!File.Exists(filePath))
        {
            fileExists = false;
            LogWarning("The image for the prop couldn't be found. Please check the names or if the file has been deleted.");
        }

        _size = Vector2.One;
        switch (type)
        {
            // unvaried soft props have implicit sizes
            case PropType.Soft:
            {
                if (!fileExists) break;

                using Image propImg = Image.Load(filePath);
                _size.X = propImg.Size.Width;
                _size.Y = propImg.Size.Height - 1;

                break;
            }

            case PropType.VariedSoft:
            {
                Vector2Int intv2;
                Match sizeStr = Regex.Match(line, @"#pxlSize *: *point\( *([0-9]+) *, *([0-9]+) *\)");
                if (!RWUtils.LingoInt(sizeStr.Groups[1].Value, out intv2.X))
                {
                    _size.X = 1;
                    LogWarning("Couldn't parse Size X component");
                }
                if (!RWUtils.LingoInt(sizeStr.Groups[2].Value, out intv2.Y))
                {
                    _size.Y = 1;
                    LogWarning("Couldn't parse Size Y component");
                }

                _size = (Vector2)intv2;

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

        // ROUND
        string roundStr = Regex.Match(line, "#round *: *([0-1]){1}").Groups[1].Value;
        if (!RWUtils.LingoBool(roundStr, out _round))
        {
            LogWarning("Round couldn't be parsed! Defaulting to no round");
            _round = false;
        }

        // SELF SHADE
        string selfShadeStr = Regex.Match(line, "#round *: *([0-1]){1}").Groups[1].Value;
        if (!RWUtils.LingoBool(selfShadeStr, out _selfShade))
        {
            LogWarning("Self shading couldn't be parsed! Defaulting to none");
            _selfShade = false;
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

        SearchName = $"{Category} {Name} {type} {string.Join(' ', _tags)} {_size.X}x{_size.Y}".ToLower();

        // soft props only repeat once
        _repeatLayers = new int[_round ? _depth : _depth * 2];
        Array.Fill(_repeatLayers, 1);

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
        _size,
        _repeatLayers,
        _variations,
        scene,
        objectManager
    );

    private RenderDescription CompleteRenderDescription(RWVertexData[] vertices, ushort[] indices, PropObject instance, Camera camera, Texture texture) => new(
        "soft_prop",
        vertices,
        indices,
        GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectTextureLayout,
                texture,
                camera.Scene.PaletteManager.CurrentPalette.DisplayTex.Texture,
                camera.Scene.PaletteManager.EffectColors.Texture,
                camera.LightingPass.DepthTexture,
                camera.RemovalPass.RenderTexture.Texture
            )
        ),
        new RWSoftPropRenderUniform(
            instance,
            _size,
            _variations,
            _colorize,
            _round,
            _selfShade ? _smoothShading : 0,
            _contourExponent,
            _highlightMin,
            _shadowMin,
            _highlightExponent
        ),
        false, true, true,
        RWUtils.RWResourceLayout, RWUtils.SoftPropShaders,
        [], []
    );

    private Vector2 GetTexPos(int var, int _) => new(_size.X * var,  1);
}