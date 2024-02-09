using System;
using System.Numerics;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Props;

public enum PropColorTreatment
{
    Standard,
    Bevel
}

[Flags]
public enum PropTags
{
    None,
    RandomRotat = 2,
    RandomFlipX = 4,
    RandomFlipY = 8,
    SnapToGrid = 16,
    PostEffects = 32,
    CircularSign = 64,
    Colored = 128,
    CustomColorRainbow = 256,
    CustomColor = 512
}

public abstract class Prop : IRWRenderable
{
    public readonly string Category;

    public readonly Vector3 CategoryColor;

    public readonly string Name;

    public readonly bool Colorize;

    public readonly PropTags Tags;

    public readonly int Variations;

    public readonly string[] Notes;

    protected Prop(string category, Vector3 categoryColor, string name)
    {
        Category = category;
        CategoryColor = categoryColor;
        Name = name;
        Colorize = false;
        Tags = PropTags.None;
        Variations = 1;
        Notes = Array.Empty<string>();
    }

    public abstract RWRenderDescription GetSceneInfo(RWScene scene);

    public abstract DeviceBuffer CreateObjectData(RWScene scene);

    public abstract Vector2Int GetRenderSize(RWScene scene);

    public abstract Vector2 GetTextureSize();

    public abstract ShaderSetDescription GetShaderSetDescription();

    public abstract int LayerCount();

    public abstract int Layer();

    public abstract bool GetTextureSet(RWScene scene, out ResourceSet textureSet);
}

/*
public class Prp : RWObject
{
    [JsonInclude] public readonly string Category;

    [JsonIgnore] public readonly Vector3 CategoryColor;

    [JsonInclude] public readonly string CategoryColorSer;

    [JsonInclude] public readonly string Name;

    [JsonInclude] public readonly PropType Type;

    [JsonInclude] public readonly PropColorTreatment ColorTreatment;

    [JsonInclude] public readonly int Bevel;

    [JsonInclude] public readonly Vector2Int Size;

    [JsonInclude] public readonly Vector2Int PixelSize;

    [JsonInclude] public readonly int[] RepeatLayers;

    [JsonInclude] public readonly int Variations;

    [JsonInclude] public readonly bool Random;

    [JsonInclude] public readonly int Depth;

    [JsonInclude] public readonly float Round;

    [JsonInclude] public readonly float ContourExp;

    [JsonInclude] public readonly bool SelfShade;

    [JsonInclude] public readonly float HighlightBorder;

    [JsonInclude] public readonly float DepthAffectHighlights;

    [JsonInclude] public readonly float ShadowBorder;

    [JsonInclude] public readonly int SmoothShading;

    [JsonInclude] public readonly bool Colorize;

    [JsonInclude] public readonly PropTags Tags;

    [JsonInclude] public readonly string[] Notes;

    public Prp(string line, string category, Vector3 categoryColor, ref string log)
    {
        /*
         * ALL ->
         * category
         * name
         * colorize
         * tags
         * notes
         *
         * STANDARD & StVARIED ->
         * tile size
         * color treatment
         *
         * _VARIED ->
         * variations
         *
         * SOFT & DECAL & ANTIMATTER ->
         * depth
         * contour exp?
         *
         * SOFT & DECAL ->
         * pixel size
         * round
         * self shading
         * highlight border
         * depth affect highlights
         * shadow border
         * smooth shading
         *

        // CATEGORIES
        Category = category;
        CategoryColor = categoryColor;
        CategoryColorSer = $"{categoryColor.X},{categoryColor.Y},{categoryColor.Z}";

        // NAME
        Name = Regex.Match(line, "#nm *: *\"(.*?)\"").Groups[1].Value;

        // TYPE
        string typeStr = Regex.Match(line, "#tp *: *\"(.*?)\"").Groups[1].Value;
        if (!RWUtils.LingoEnum(typeStr, PropType.Standard, out PropType type))
        {
            log += $"Couldn't parse prop type '{typeStr}' on prop {Name}! Defaulting to Standard.\n";
        }
        Type = type;

        // TAGS

        if (Type is
            // Standard implements:
            // tile size
            // color treatment
            PropType.Standard or PropType.VariedStandard)
        {
            // SIZE IN TILE UNITS (20px per unit)
            Match sizeStr = Regex.Match(line, @"#sz *: *point\( *([0-9]+) *, *([0-9]+) *\)");
            RWUtils.LingoInt(sizeStr.Groups[1].Value, out int sizeX);
            RWUtils.LingoInt(sizeStr.Groups[2].Value, out int sizeY);
            Size = new Vector2Int(sizeX, sizeY);

            string colorTreatment = Regex.Match(line, "#colorTreatment *: *\"(.*?)\"").Groups[1].Value;
            if (!RWUtils.LingoEnum(colorTreatment, PropColorTreatment.Standard, out ColorTreatment))
            {
                log +=
                    $"Couldn't parse prop color treatment '{colorTreatment}' on prop {Name}! Defaulting to Standard.\n";
            }
        }

        // _Varied implements:
        // variations
        if (Type is PropType.VariedStandard or PropType.VariedSoft or PropType.VariedDecal)
        {
            // VARIATIONS
            string vars = Regex.Match(line, @"#vars *: *([0-9])").Groups[1].Value;
            if (!RWUtils.LingoInt(vars, out Variations))
            {
                log += $"Couldn't parse vars on prop {Name}!\n";
            }
        }

        // Varied Soft implements:
        // pixel size [Vector2Int]
        if (Type is PropType.VariedSoft)
        {
            // SIZE IN PIXELS
            Match sizeStr = Regex.Match(line, @"#sz *: *point\( *([0-9]+) *, *([0-9]+) *\)");
            RWUtils.LingoInt(sizeStr.Groups[1].Value, out int sizeX);
            RWUtils.LingoInt(sizeStr.Groups[2].Value, out int sizeY);
            Size = new Vector2Int(sizeX, sizeY);
        }

        // Soft implements:
        // depth [int]
        // round [float]
        // contour exp?
        // self shading
        // highlight border
        // depth affect highlights
        // shadow border
        // smooth shading
        if (Type is PropType.Soft)
        {

        }

        // TODO: PROPS
    }
}
*/
