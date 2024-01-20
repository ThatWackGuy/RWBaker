using System.Numerics;
using System.Runtime.InteropServices;
using RWBaker.GeneralTools;
using RWBaker.TileTools;

namespace RWBaker.GraphicsTools;

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
        EffectColorsTexSize = PaletteManager.EffectColors.Size; // 8
        // 16 bytes total

        EffectA = PaletteManager.Context.EffectColorA; // 4
        EffectB = PaletteManager.Context.EffectColorB; // 4
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

    public RWTileRenderUniform(Tile tile)
    {
        TileSize = (Vector2)tile.Size;
        BufferTiles = tile.BufferTiles;
        Vars = tile.Variants;
        UseRainPalette = tile.UseRainPalette ? 1 : 0;
        IsBox = tile.Type == TileType.Box ? 1 : 0;
    }
}