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

    public RWShadowRenderUniform(RWScene scene, IRWRenderable renderable)
    {
        Projection = scene.Transform;
        LightOffset = scene.LightOffset;
        ObjectOffset = scene.ObjectOffset;
        TexSize = renderable.GetTextureSize();
        LayerCount = renderable.LayerCount();
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWTileRenderUniform
{
    public readonly Matrix4x4 Projection;
    public readonly Vector2 Offset;
    public readonly Vector2 TexSize;
    public readonly Vector2 ShTexSize;
    public readonly bool UseRainPalette;
    public readonly int LayerCount;

    public RWTileRenderUniform(RWScene scene, Tile tile)
    {
        Projection = scene.Transform;
        Offset = scene.ObjectOffset;
        TexSize = tile.GetTextureSize();
        ShTexSize = new Vector2(scene.Width, scene.Height);
        UseRainPalette = tile.UseRainPalette;
        LayerCount = tile.LayerCount();
    }
}