using System.Numerics;
using System.Runtime.InteropServices;
using RWBaker.GeneralTools;
using RWBaker.TileTools;

namespace RWBaker.GraphicsTools;

[StructLayout(LayoutKind.Sequential)]
public struct RWStandardRenderUniform
{
    public readonly Matrix4x4 Projection;
    public readonly Vector2 Offset;
    public readonly Vector2 TexSize;

    public RWStandardRenderUniform(Matrix4x4 proj, Vector2 offset, Vector2 texSize)
    {
        Projection = proj;
        Offset = offset;
        TexSize = texSize;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWShadowRenderUniform
{
    public readonly Matrix4x4 Projection;
    public readonly Vector2 LightOffset;
    public readonly Vector2 ObjectOffset;
    public readonly Vector2 TexSize;
    public readonly int LayerCount;
    public float RepeatCurrent;
    public readonly float RepeatMax;

    public RWShadowRenderUniform(RWScene scene, IRWRenderable renderable)
    {
        Projection = scene.Transform;
        LightOffset = scene.LightOffset;
        ObjectOffset = scene.ObjectOffset;
        TexSize = renderable.GetTextureSize();
        LayerCount = renderable.LayerCount();
        RepeatCurrent = 0;
        RepeatMax = scene.ShadowRepeat;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWTileRenderUniform
{
    public readonly Matrix4x4 Projection;
    public readonly Vector2 Offset;
    public readonly Vector2 TexSize;
    public readonly Vector2 ShTexSize;
    public readonly Vector2 TileSize;
    public readonly int BufferTiles;
    public readonly bool UseRainPalette;
    public readonly int LayerCount;
    public readonly int PaletteLayer;
    public readonly bool IsBox;

    public RWTileRenderUniform(RWScene scene, Tile tile)
    {
        Projection = scene.Transform;
        Offset = scene.ObjectOffset;
        TexSize = tile.GetTextureSize();
        ShTexSize = new Vector2(scene.Width, scene.Height);
        TileSize = (Vector2)tile.Size;
        BufferTiles = tile.BufferTiles;
        UseRainPalette = tile.UseRainPalette;
        LayerCount = tile.LayerCount();
        PaletteLayer = tile.RenderLayer;
        IsBox = tile.Type == TileType.Box;
    }
}