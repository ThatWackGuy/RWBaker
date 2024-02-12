using System.Numerics;
using System.Runtime.InteropServices;
using RWBaker.RWObjects;
using RWBaker.Tiles;

namespace RWBaker.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct RWSceneInfo
{
    public readonly Matrix4x4 Transform;
    public readonly float Layer;
    public readonly float LayerCount;
    public readonly Vector2 ObjectOffset;

    public bool RenderingShadows;
    public float CurrentRepeat;
    public readonly float MaxRepeat;
    public readonly float PADDING = 0f;

    public readonly Vector2 LightOffset;
    public readonly Vector2 ShadowTexSize;

    public readonly Vector2 RenderableTexSize;

    public readonly Vector2 EffectColorsTexSize;
    public readonly float EffectA;
    public readonly float EffectB;

    public RWSceneInfo(RWScene scene, IRWRenderable renderable)
    {
        Transform = scene.Transform; // 64 bytes

        Layer = renderable.Layer(); // 4
        LayerCount = renderable.LayerCount(); // 4
        ObjectOffset = scene.ObjectOffset; // 8
        // 16 bytes total

        RenderingShadows = true; // 4
        CurrentRepeat = 0; // 4
        MaxRepeat = scene.ShadowRepeat; // 4
        // PADDING 4
        // 16 bytes total

        LightOffset = scene.LightOffset; // 8
        ShadowTexSize = new Vector2(scene.Width, scene.Height); // 8
        // 16 bytes total

        RenderableTexSize = renderable.GetTextureSize(); // 8
        EffectColorsTexSize = scene.Palettes.EffectColors.Size; // 8
        // 16 bytes total

        EffectA = scene.Palettes.EffectColorA; // 4
        EffectB = scene.Palettes.EffectColorB; // 4
        // 8 bytes total
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWTileRenderUniform
{
    public readonly Vector2 TileSize;
    public readonly int BufferTiles;
    public readonly int Vars;
    public readonly int UseRainPalette;
    public readonly int IsBox;

    public RWTileRenderUniform(CachedTile cachedTile)
    {
        TileSize = (Vector2)cachedTile.Size;
        BufferTiles = cachedTile.BufferTiles;
        Vars = cachedTile.Variants;
        UseRainPalette = cachedTile.UseRainPalette ? 1 : 0;
        IsBox = cachedTile.Type == TileType.Box ? 1 : 0;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWStandardPropRenderUniform
{
    public readonly Vector2 PixelSize;
    public readonly int Vars;
    public readonly int Bevel;
    public readonly int Colored;
    public readonly int UseRainPalette;

    public RWStandardPropRenderUniform(Vector2 size, int variants, int bevel, bool colored, bool rain)
    {
        PixelSize = size;
        Vars = variants;
        Bevel = bevel;
        Colored = colored ? 1 : 0;
        UseRainPalette = rain ? 1 : 0;
    }

    public void Update()
    {

    }
}