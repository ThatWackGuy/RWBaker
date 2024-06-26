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

public class AntimatterProp : Prop
{
    // TODO: Fix antimatter props
    // use < -1 depth for "remove" depth?
    // new texture for "remove" depth?

    private readonly PropTag _tags;

    private readonly string[] _notes;

    // soft prop fields
    private readonly Vector2 _size;
    private readonly int _depth;
    private readonly float _contourExponent;
    private readonly int[] _repeatLayers;

    public AntimatterProp(RWObjectManager manager, PropType type, string line, string category, Vector3 categoryColor)
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

        // CONTOUR EXP
        string contourExpStr = Regex.Match(line, "#contourExp *: *([0-9]+\\.?(?:[0-9]+)?)*").Groups[1].Value;
        if (!RWUtils.LingoFloat(contourExpStr, out _contourExponent))
        {
            LogWarning("Couldn't parse contour exponent (contourExp). Defaulting to 1.");
            _contourExponent = 1;
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

        // antimatter props only repeat once
        _repeatLayers = new int[_depth];
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
        _size * 20,
        _repeatLayers,
        1,
        scene,
        objectManager
    );

    private RenderDescription CompleteRenderDescription(Mesh mesh, Vector3 position, Matrix4x4 rotation, PropObject instance, Camera camera, Texture texture) => new(
        mesh, position, rotation,
        GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.AntimatterPropTextureLayout,
                texture
            )
        ),
        new RWAntimatterPropRenderUniform(
            instance,
            _contourExponent,
            _size
        ),
        true, false, false,
        RWUtils.AntimatterPropLayouts, RWUtils.AntimatterPropShaders,
        [], []
    );

    private Vector2 GetTexPos(int var, int layer) => Vector2.UnitY;
}