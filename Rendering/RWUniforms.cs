using System.Numerics;
using System.Runtime.InteropServices;
using RWBaker.Palettes;
using RWBaker.Props;
using RWBaker.Tiles;

namespace RWBaker.Rendering;

#region Voxeliser

[StructLayout(LayoutKind.Sequential)]
public struct VoxeliserInput
{
    public readonly Vector2 SizePerLayer;
    public readonly ushort[] RepeatLayers;

    public VoxeliserInput(Vector2 sizePerLayer, ushort[] repeatLayers)
    {
        SizePerLayer = sizePerLayer; // 8
        RepeatLayers = repeatLayers; // 4 * size
    }
}

#endregion

#region Scene Uniforms
[StructLayout(LayoutKind.Sequential)]
public struct CameraUniform
{
    public readonly Matrix4x4 Transform;

    public CameraUniform(Camera camera)
    {
        Transform = camera.ProjectionView; // 64 bytes
        // 64 bytes total
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PassUniform
{
    public readonly float Index;
    public readonly float PADDING = 0f;
    public readonly Vector2 Size;

    public PassUniform(RenderPass pass, int idx)
    {
        Index = idx; // 4 bytes
        // PADDING 4 bytes
        Size = new Vector2(pass.Framebuffer.Width, pass.Framebuffer.Height); // 8 bytes
        // 16 bytes total
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct LightingUniform
{
    public readonly Matrix4x4 LightTransform;
    public readonly Matrix4x4 LightBias;
    public readonly float ShadowBias;
    public readonly float RainPercentage;

    public LightingUniform(Camera camera)
    {
        LightTransform = camera.LightingProjectionView; // 64
        LightBias = new Matrix4x4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.5f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f
        ); // 64

        ShadowBias = camera.ShadowBias; // 4
        RainPercentage = camera.RainPercentage; // 4
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct PaletteUniform
{
    public readonly Vector2 EffectColorsTexSize;
    public readonly uint EffectA;
    public readonly uint EffectB;

    public PaletteUniform(PaletteManager paletteManager)
    {
        EffectColorsTexSize = paletteManager.EffectColors.Size; // 8
        EffectA = (uint)paletteManager.EffectColorA; // 4
        EffectB = (uint)paletteManager.EffectColorB; // 4
        // 16 bytes total
    }
}
#endregion

[StructLayout(LayoutKind.Sequential)]
public struct RWTileRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 RenderableTexSize;

    public readonly Vector2 TileSize;
    public readonly int BufferTiles;
    public readonly int Vars;
    public readonly int IsBox;

    public RWTileRenderUniform(TileObject tile)
    {
        StartingLayer = tile.Position.Z * 10; // 4
        LayerCount = tile.LayerCount; // 4
        RenderableTexSize = tile.CachedTexture.Size; // 8
        // 16 bytes

        TileSize = tile.OriginalSize;
        BufferTiles = tile.BufferTiles;
        Vars = tile.Variants;
        IsBox = tile.Type == TileType.Box ? 1 : 0;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWStandardPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 TexSize;

    public readonly Vector2 PixelSize;
    public readonly int Vars;
    public readonly int Bevel;
    public readonly int Colored;

    public RWStandardPropRenderUniform(PropObject prop, Vector2 size, int variants, int bevel, bool colored)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        TexSize = prop.CachedTexture.Size; // 8
        // 16 bytes

        PixelSize = size; // 8
        Vars = variants; // 4
        Bevel = bevel; // 4
        Colored = colored ? 1 : 0; // 4
        // 20 bytes
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWSoftPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 TexSize;

    public readonly Vector2 PixelSize;
    public readonly int Vars;
    public readonly int Colored;
    public readonly int Round;
    public readonly int SmoothShading;
    public readonly float ContourExponent;
    public readonly float HighlightMin;
    public readonly float ShadowMin;
    public readonly float DepthHighlightExponent;

    public RWSoftPropRenderUniform(PropObject prop, Vector2 size, int variants, bool colored, bool round, int smoothShading, float contourExponent, float highlightMin, float shadowMin, float depthHighlightExponent)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        TexSize = prop.CachedTexture.Size; // 8
        // 16 bytes

        PixelSize = size; // 8
        Vars = variants; // 4
        Colored = colored ? 1 : 0;
        Round = round ? 2 : 1;
        SmoothShading = smoothShading;
        ContourExponent = contourExponent;
        HighlightMin = highlightMin;
        ShadowMin = shadowMin;
        DepthHighlightExponent = depthHighlightExponent;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWAntimatterPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 TexSize;

    public readonly Vector2 PixelSize;
    public readonly float ContourExponent;

    public RWAntimatterPropRenderUniform(PropObject prop, float contourExponent,  Vector2 size)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        TexSize = prop.CachedTexture.Size; // 8
        // 16 bytes

        PixelSize = size;
        ContourExponent = contourExponent;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWDecalPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 TexSize;

    public readonly Vector2 PixelSize;

    public RWDecalPropRenderUniform(PropObject prop, Vector2 size)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        TexSize = prop.CachedTexture.Size; // 8
        // 16 bytes

        PixelSize = size;
    }
}