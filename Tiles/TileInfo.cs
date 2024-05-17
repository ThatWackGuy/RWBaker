using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace RWBaker.Tiles;

public enum TileType
{
    VoxelStruct,
    VoxelStructRockType,
    VoxelStructRandomDisplaceHorizontal,
    VoxelStructRandomDisplaceVertical,
    Box,
}

public enum TileTag
{
    None,
    RandomRotat,
    DrawLast,
    Ramp,
    ChainHolder,
    FanBlade,
    SawBlades,
    BigWheel,

    BigSign,
    BigSignB,

    LargerSign,
    LargerSignB,

    BigWesternSign,
    BigWesternSignB,

    BigWesternSignTilted,
    BigWesternSignTiltedB,

    SmallAsianSign,
    SmallAsianSignB,

    SmallAsianSignOnWall,
    SmallAsianSignOnWallB,

    Glass,
    Harvester,
    TempleFloor,
    NonSolid,
    NotProp,
    NotTrashProp,

    // Community Editor
    EffectColorA,
    EffectColorB
}

public class TileInfo
{
    public bool HasWarnings;
    public string Warnings;

    public readonly string Category;

    public readonly Vector3 CategoryColor;

    public readonly string Name;

    public readonly string ProperName; // Category - Name

    public readonly string SearchName; // Category Name Type Tags Size [all lowercase]

    public readonly Vector2Int Size;

    public readonly int[,,] Specifications;

    public readonly bool HasSpecs2;

    public readonly TileType Type;

    public readonly int[] RepeatLayers;

    public readonly int BufferTiles;

    public readonly int Variants;

    public readonly int PtPos; // ????

    public readonly TileTag[] Tags;

    public TileInfo(RWObjectManager manager, string line, string category, Vector3 categoryColor)
    {
        HasWarnings = false;
        Warnings = "";

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
        Size = new Vector2Int(sizeX, sizeY);

        // Get specifications
        string[] specifications1 = Regex.Match(line, @"#specs *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");
        string[] specifications2 = Regex.Match(line, @"#specs2 *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");
        Specifications = new int[sizeX, sizeY, 2];

        int x = 0;
        int y = 0;

        if (specifications1[0] == "")
        {
            specifications1 = Array.Empty<string>();
            LogWarning($"Specs should not be empty! Specs couldn't be parsed. Defaulting to empty.");
        }

        if (specifications2[0] == "")
        {
            specifications2 = Array.Empty<string>();
        }
        else
        {
            HasSpecs2 = true;
        }

        foreach (string spec in specifications1)
        {
            Specifications[x, y, 0] = int.Parse(spec);

            x++;

            if (x < sizeX) continue;
            x = 0;
            y++;

            if (y < sizeY) continue;
            y = 0;
        }

        x = 0;
        y = 0;
        foreach (string spec in specifications2)
        {
            Specifications[x, y, 1] = int.Parse(spec);

            x++;

            if (x < sizeX) continue;
            x = 0;
            y++;

            if (y < sizeY) continue;
            y = 0;
        }

        // TILE TYPE
        string typeStr = Regex.Match(line, "#tp *: *\"(.*?)\"").Groups[1].Value;
        if (!RWUtils.LingoEnum(typeStr, TileType.VoxelStruct, out Type))
        {
            LogWarning($"Couldn't parse tile type '{typeStr}'. Defaulting to VoxelStruct.");
        }

        // REPEATING LAYERS
        string[] repeatLayers = Regex.Match(line, @"#repeatL *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");

        if (!RWUtils.LingoIntArray(repeatLayers, out RepeatLayers))
        {
            switch (Type)
            {
                case TileType.VoxelStructRockType:
                {
                    RepeatLayers = new[] { 10 };
                    break;
                }

                // Boxes take up the entire layer
                case TileType.Box:
                {
                    RepeatLayers = new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                    break;
                }

                default:
                {
                    LogWarning("Couldn't parse repeatL");
                    break;
                }
            }
        }

        if (RepeatLayers.Length > 30)
        {
            LogWarning($"30+ layers aren't supported! {repeatLayers.Length} layers on tile.");
        }

        if (RepeatLayers.Sum() >= 30)
        {
            LogWarning($"30+ sub-layers aren't supported! {RepeatLayers.Sum()} sub-layers on tile.");
        }

        // BUFFER TILES
        string bfTiles = Regex.Match(line, @"#bfTiles *: *([0-9])").Groups[1].Value;
        if (!RWUtils.LingoInt(bfTiles, out BufferTiles))
        {
            LogWarning("Couldn't parse bfTiles.");
        }

        // VARIATIONS
        string rnd = Regex.Match(line, @"#rnd *: *([0-9]*)").Groups[1].Value;
        if (!RWUtils.LingoInt(rnd, out Variants))
        {
            LogWarning("Couldn't parse rnd.");
            Variants = 1;
        }

        // WHATEVER THIS IS
        string ptPos = Regex.Match(line, @"#ptPos *: *([0-9])").Groups[1].Value;
        if (!RWUtils.LingoInt(ptPos, out PtPos))
        {
            LogWarning("Couldn't parse ptPos.");
        }

        // TAGS
        string tagsRaw = Regex.Match(line, @"#tags *: *\[(.*?)\]").Groups[1].Value;
        string[] tagsList = Regex.Matches(tagsRaw, "\"(.*?)\"").Select(m => m.Groups[1].Value).ToArray();

        Tags = new TileTag[tagsList.Length];
        for (int i = 0; i < tagsList.Length; i++)
        {
            if (!RWUtils.LingoEnum(tagsList[i].Replace(" ", ""), TileTag.None, out TileTag tag))
            {
                LogWarning($"Couldn't parse tile tag '{tagsList[i]}'.");
            }

            Tags[i] = tag;
        }

        // SEARCH NAME
        SearchName = $"{Category} {Name} {Type} {string.Join(' ', Tags)} {sizeX}x{sizeY}".ToLower();

        if (!File.Exists(manager.GraphicsDir + "/" + Name + ".png"))
        {
            LogWarning("The image for the tile couldn't be found. Please check the names or if the file has been deleted.");
        }

        if (HasWarnings)
        {
            manager.TileLoadLogs += ProperName + ":\n" + Warnings + "\n";
        }
    }

    private void LogWarning(string warn)
    {
        HasWarnings = true;
        Warnings += "\t" + warn  + "\n";
    }
}