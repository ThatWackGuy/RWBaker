using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;
using Veldrid;

namespace RWBaker.TileTools;

public enum TileType
{
    VoxelStruct = 1,
    VoxelStructRockType = 2,
    VoxelStructRandomDisplaceHorizontal = 4,
    VoxelStructRandomDisplaceVertical = 8,
    Box = 16,
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
    NotTrashProp
}

public class Tile : RWObject, IRWRenderable
{
    [JsonInclude]
    public readonly string Category;
    
    [JsonIgnore]
    public readonly Vector3 CategoryColor;
    
    [JsonInclude]
    public readonly string CategoryColorSer;
    
    [JsonInclude]
    public readonly string Name;
    
    [JsonInclude]
    public readonly Vector2Int Size;

    [JsonIgnore]
    public readonly int[,,] Specifications;
    
    [JsonInclude]
    public readonly int[] SpecificationsFlattened;
    
    [JsonInclude]
    public readonly TileType Type;
    
    [JsonInclude]
    public readonly int[] RepeatLayers;

    [JsonInclude]
    public readonly int BufferTiles;
    
    [JsonInclude]
    public readonly int Variants;
    
    [JsonInclude]
    public readonly int PtPos; // ????
    
    [JsonIgnore]
    public readonly TileTag[] Tags;
    
    [JsonInclude]
    public string[] TagsString => Tags.Select(t => $"{t}").ToArray();

    [JsonIgnore]
    public string FullName => $"{Category}_{Name}";
    
    [JsonIgnore]
    public string ProperName => $"{Category} - {Name}";

    // Render variables
    [JsonIgnore] public Texture? CachedTexture;
    
    [JsonIgnore] private readonly int[] renderRepeatLayers;
    
    [JsonIgnore] public Vector2Int PixelSize => Size * 20 + BufferTiles * 40;
    
    [JsonIgnore] private int renderVariation;
    
    [JsonIgnore] public bool UseRainPalette;
    
    public Tile()
    {
        
    }

    public Tile(string line, string category, Vector3 categoryColor, ref string log)
    {
        // CATEGORY
        Category = category;
        CategoryColor = categoryColor;
        CategoryColorSer = $"{categoryColor.X},{categoryColor.Y},{categoryColor.Z}";
        
        // NAME
        Name = Regex.Match(line, "#nm *: *\"(.*?)\"").Groups[1].Value;

        // SIZE
        Match sizeStr = Regex.Match(line, @"#sz *: *point\( *([0-9]+) *, *([0-9]+) *\)");
        int sizeX = int.Parse(sizeStr.Groups[1].Value);
        int sizeY = int.Parse(sizeStr.Groups[2].Value);
        Size = new Vector2Int(sizeX, sizeY);
        
        // SPECIFICATIONS
        string[] specifications1 = Regex.Match(line, @"#specs *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");
        string[] specifications2 = Regex.Match(line, @"#specs2 *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");
        
        Specifications = new int[sizeX, sizeY, 2];
        
        int x = 0;
        int y = 0;
        
        if (specifications1[0] == "")
        {
            specifications1 = Array.Empty<string>();
            LogWarning($"Specs should not be empty! Specs couldn't be parsed on tile {Name}! Defaulting to empty.", ref log);
        }
        
        if (specifications2[0] == "") specifications2 = Array.Empty<string>();
        
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
        
        foreach (string spec in specifications2)
        {
            Specifications[x, y, 1] = int.Parse(spec);
            
            x++;
            
            if (x < sizeX) continue;
            x = 0;
            y++;
        }

        // Flatten for serialization
        x = 0;
        y = 0;
        int z = 0;
        SpecificationsFlattened = new int[Specifications.LongLength];
        for (int i = 0; i < SpecificationsFlattened.Length; i++)
        {
            SpecificationsFlattened[i] = Specifications[x, y, z];
            
            x++;
            if (x < sizeX) continue;
            x = 0;
            y++;

            if (y < sizeY) continue;
            y = 0;
            z++;
        }

        // TILE TYPE
        string typeStr = Regex.Match(line, "#tp *: *\"(.*?)\"").Groups[1].Value;
        if (!RWUtils.LingoEnum(typeStr, TileType.VoxelStruct, out Type))
        {
            LogWarning($"Couldn't parse tile type '{typeStr}' on tile {Name}! Defaulting to VoxelStruct.", ref log);
        }

        // REPEATING LAYERS
        string[] repeatLayers = Regex.Match(line, @"#repeatL *: *\[(.*?)\]").Groups[1].Value.Replace(" ", "").Split(",");

        if (!RWUtils.LingoIntArray(repeatLayers, out RepeatLayers))
        {
            if (Type is TileType.VoxelStructRockType or TileType.Box)
            {
                RepeatLayers = new[] { 1 };
            }
            else
            {
                LogWarning($"Couldn't parse repeatL on tile {Name}!", ref log);
            }
        }
        renderRepeatLayers = RepeatLayers;
        
        if (RepeatLayers.Length > 30)
        {
            LogWarning($"30+ layers aren't supported! {repeatLayers.Length} layers on tile {Name}!", ref log);
            renderRepeatLayers = RepeatLayers[..30];
        }

        if (renderRepeatLayers.Sum() >= 30)
        {
            LogWarning($"30+ sub-layers aren't supported! {RepeatLayers.Sum()} sub-layers on tile {Name}!", ref log);

            int check = 0;
            for (int i = 0; i < renderRepeatLayers.Length; i++)
            {
                int repeat = renderRepeatLayers[i];

                if (check + repeat > 30)
                {
                    renderRepeatLayers = renderRepeatLayers[..i];
                    Array.Resize(ref renderRepeatLayers, i + 1);
                    renderRepeatLayers[i] = 30 - check;
                    break;
                }

                check += repeat;
            }
        }

        // BUFFER TILES
        string bfTiles = Regex.Match(line, @"#bfTiles *: *([0-9])").Groups[1].Value;
        if (!RWUtils.LingoInt(bfTiles, out BufferTiles))
        {
            LogWarning( $"Couldn't parse bfTiles on tile {Name}!", ref log);
        }

        // VARIATIONS
        string rnd = Regex.Match(line, @"#rnd *: *([0-9])").Groups[1].Value;
        if (!RWUtils.LingoInt(rnd, out Variants))
        {
            LogWarning( $"Couldn't parse rnd on tile {Name}!", ref log);
            Variants = 1;
        }
        
        // WHATEVER THIS IS
        string ptPos = Regex.Match(line, @"#ptPos *: *([0-9])").Groups[1].Value;
        if (!RWUtils.LingoInt(ptPos, out PtPos))
        {
            LogWarning($"Couldn't parse ptPos on tile {Name}!", ref log);
        }

        // TAGS
        string tagsRaw = Regex.Match(line, @"#tags *: *\[(.*?)\]").Groups[1].Value;
        string[] tagsList = Regex.Matches(tagsRaw, "\"(.*?)\"").Select(m => m.Groups[1].Value).ToArray();

        Tags = new TileTag[tagsList.Length];
        for (int i = 0; i < tagsList.Length; i++)
        {
            if (!RWUtils.LingoEnum(tagsList[i].Replace(" ", ""), TileTag.None, out TileTag tag))
            {
                LogWarning($"Couldn't parse tile tag '{tagsList[i]}' on tile {Name}!", ref log);
            }
            
            Tags[i] = tag;
        }
    }

    public (int[], int[]) CollapseSpecs(ref string log)
    {
        if (Specifications.LongLength % 2 != 0)
        {
            log += $"Specs of {Name} either do not match up or the tile size is wrong! Please check your specs!\n";
            return (Array.Empty<int>(), Array.Empty<int>());
        }
        
        int x = 0;
        int y = 0;
        int [] specs1 = new int[Specifications.LongLength / 2];
        for (int i = 0; i < specs1.Length; i++)
        {
            specs1[i] = Specifications[x, y, 0];
            
            x++;
            if (x < Size.X) continue;
            x = 0;
            y++;

            if (y < Size.Y) continue;
            y = 0;
        }

        x = 0;
        y = 0;
        int [] specs2 = new int[Specifications.LongLength / 2];
        for (int i = 0; i < specs2.Length; i++)
        {
            specs2[i] = Specifications[x, y, 1];
            
            x++;
            if (x < Size.X) continue;
            x = 0;
            y++;

            if (y < Size.Y) continue;
            y = 0;
        }

        return (specs1, specs2);
    }

    public RWRenderDescription GetSceneInfo(RWScene scene)
    {
        return Type switch
        {
            TileType.VoxelStruct or TileType.VoxelStructRockType or TileType.VoxelStructRandomDisplaceHorizontal or TileType.VoxelStructRandomDisplaceVertical => VoxelInfo(scene),
            TileType.Box => BoxInfo(scene),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public DeviceBuffer CreateObjectData(RWScene scene)
    {
        DeviceBuffer buffer = GuiManager.ResourceFactory.CreateStructBuffer<RWTileRenderUniform>();
        
        GuiManager.GraphicsDevice.UpdateBuffer(buffer, 0, new RWTileRenderUniform(scene, this));

        return buffer;
    }
    
    public ShaderSetDescription GetShaderSetDescription() => RWUtils.TileRendererShaderSet;

    public int LayerCount() => renderRepeatLayers.Sum();

    public Vector2Int GetRenderSize(RWScene scene) => PixelSize + (renderRepeatLayers.Length - 1) * Vector2.Abs(scene.ObjectOffset);

    public Vector2 GetTextureSize() => new(CachedTexture!.Width, CachedTexture!.Height);
    
    public bool GetTextureSet(RWScene scene, [MaybeNullWhen(false)] out ResourceSet textureSet)
    {
        textureSet = GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectTextureLayout,
                CachedTexture!,
                Program.CurrentPalette.DisplayTex.Texture,
                scene.ShadowRender.Texture
            )
        );

        return true;
    }

    private RWRenderDescription VoxelInfo(RWScene scene)
    {
        if (CachedTexture == null) throw new NullReferenceException("Cached tile texture does not exist");

        int layerCount = renderRepeatLayers.Sum();
        var vertices = new RWVertexData[layerCount * 4];
        ushort[] indices = new ushort[layerCount * 6];
        
        int vertIndex = 0;
        int indexIndex = 0;
        for (int imgLayer = 0; imgLayer < renderRepeatLayers.Length; imgLayer++)
        {
            if (renderRepeatLayers[imgLayer] == 0) continue;

            // Vector2 vertPos = new Vector2(float.Max(per.X * -1, 0), float.Max(per.Y * -1, 0)) * (renderRepeatLayers.Length - 1) + per * imgLayer;
            Vector2 texPos = new(PixelSize.X * renderVariation,  imgLayer * PixelSize.Y);
            Vector2 texSize = (Vector2)PixelSize;
            
            for (int repeat = 0; repeat < renderRepeatLayers[imgLayer]; repeat++)
            {
                // TODO: FIX REPEATS
                // each layer need to be equally offset from other layers
                // both shaders work on the assumption that tile layers do not repeat
                // find a way to minimise data AND properly stack each layer
                float renderLayer = 1 + imgLayer + repeat;

                vertices[vertIndex] = new RWVertexData(
                    new Vector3(Vector2.Zero, renderLayer),
                    texPos,
                    RgbaFloat.Clear
                ); // Top Left
                
                vertices[vertIndex + 1] = new RWVertexData(
                    new Vector3(PixelSize.X, 0, renderLayer),
                    texPos with { X = texPos.X + texSize.X },
                    RgbaFloat.Clear
                ); // Top Right

                vertices[vertIndex + 2] = new RWVertexData(
                    new Vector3(PixelSize.X, PixelSize.Y, renderLayer),
                    texPos + texSize,
                    RgbaFloat.Clear
                ); // Bottom Right
                
                vertices[vertIndex + 3] = new RWVertexData(
                    new Vector3(0, PixelSize.Y, renderLayer),
                    texPos with { Y = texPos.Y + texSize.Y },
                    RgbaFloat.Clear
                ); // Bottom Left

                // 0 1 2, 2 3 0 for each quad
                indices[indexIndex]     = (ushort)vertIndex;       // 0
                indices[indexIndex + 1] = (ushort)(vertIndex + 1); // 1
                indices[indexIndex + 2] = (ushort)(vertIndex + 2); // 2
                indices[indexIndex + 3] = (ushort)(vertIndex + 2); // 2
                indices[indexIndex + 4] = (ushort)(vertIndex + 3); // 3
                indices[indexIndex + 5] = (ushort)(vertIndex + 0); // 0
                
                vertIndex += 4;
                indexIndex += 6;
            }
        }

        return new RWRenderDescription(vertices, indices, this, scene);
    }

    private RWRenderDescription BoxInfo(RWScene scene)
    {
        // TODO: TILE BOX RENDERING
        throw new NotImplementedException();
    }

    public void CacheTexture(Context context)
    {
        if (File.Exists(context.SavedGraphicsDir + "/" + Name + ".png"))
        {
            CachedTexture = GuiManager.TextureFromImage(context.SavedGraphicsDir + "/" + Name + ".png");
        }
    }

    public void Variation(int var) => renderVariation = int.Clamp(var, 0, Variants - 1);
}