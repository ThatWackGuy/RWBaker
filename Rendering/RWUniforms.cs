using System.Numerics;
using System.Runtime.InteropServices;
using RWBaker.Props;
using RWBaker.Tiles;

namespace RWBaker.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct RWSceneInfo
{
    public readonly Matrix4x4 Transform;
    public readonly Vector2 ObjectOffset;

    public bool RenderingShadows;
    public float CurrentRepeat;
    public readonly float MaxRepeat;
    public readonly float PADDING = 0f;

    public readonly Vector2 LightOffset;
    public readonly Vector2 ShadowTexSize;

    public readonly Vector2 EffectColorsTexSize;
    public readonly float EffectA;
    public readonly float EffectB;

    public RWSceneInfo(Scene scene, int currentRepeat, bool renderingShadows)
    {
        Transform = scene.Projection; // 64 bytes
        ObjectOffset = scene.ObjectOffset; // 8
        // 72 bytes total

        RenderingShadows = renderingShadows; // 4
        CurrentRepeat = currentRepeat; // 4
        MaxRepeat = scene.ShadowRepeat; // 4
        // PADDING 4
        // 16 bytes total

        LightOffset = scene.LightAngle;
        ShadowTexSize = new Vector2(scene.Width, scene.Height); // 8
        // 16 bytes total

        EffectColorsTexSize = scene.PaletteManager.EffectColors.Size; // 8
        EffectA = scene.PaletteManager.EffectColorA; // 4
        EffectB = scene.PaletteManager.EffectColorB; // 4
        // 16 bytes total
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWTileRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 RenderableTexSize;

    public readonly Vector2 TileSize;
    public readonly int BufferTiles;
    public readonly int Vars;
    public readonly int UseRainPalette;
    public readonly int IsBox;

    public RWTileRenderUniform(TileObject tile)
    {
        StartingLayer = tile.Position.Z * 10; // 4
        LayerCount = tile.LayerCount; // 4
        RenderableTexSize = new Vector2(tile.CachedTexture.Width, tile.CachedTexture.Height); // 8
        // 16 bytes

        TileSize = (Vector2)tile.Size;
        BufferTiles = tile.BufferTiles;
        Vars = tile.Variants;
        UseRainPalette = tile.Scene.Rain ? 1 : 0;
        IsBox = tile.Type == TileType.Box ? 1 : 0;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWStandardPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 TexSize;

    public readonly Matrix4x4 Rotation;
    public readonly Vector2 PixelSize;
    public readonly int Vars;
    public readonly int Bevel;
    public readonly int Colored;
    public readonly int UseRainPalette;

    public RWStandardPropRenderUniform(PropObject prop, Vector2 size, int variants, int bevel, bool colored)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        TexSize = new Vector2(prop.CachedTexture.Width, prop.CachedTexture.Height); // 8
        // 16 bytes

        Rotation = Matrix4x4.CreateRotationZ(prop.Rotation/* * (float.Pi / 180)*/, prop.Position + new Vector3((Vector2)prop.PixelSize, 1)/ 2); // 64
        PixelSize = size; // 8
        Vars = variants; // 4
        Bevel = bevel; // 4
        Colored = colored ? 1 : 0; // 4
        UseRainPalette = prop.Scene.Rain ? 1 : 0; // 4
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct RWSoftPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 RenderableTexSize;

    public readonly Matrix4x4 Rotation;
    public readonly Vector2 PixelSize;
    public readonly int Vars;
    public readonly int Colored;
    public readonly int SmoothShading;
    public readonly float ContourExponent;
    public readonly float HighlightMin;
    public readonly float ShadowMin;
    public readonly float DepthHighlightExponent;
    public readonly int UseRainPalette;

    public RWSoftPropRenderUniform(PropObject prop, Vector2 size, int variants, bool colored, int smoothShading, float contourExponent, float highlightMin, float shadowMin, float depthHighlightExponent)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        RenderableTexSize = new Vector2(prop.CachedTexture.Width, prop.CachedTexture.Height); // 8
        // 16 bytes

        Rotation = Matrix4x4.CreateRotationZ(prop.Rotation/* * (float.Pi / 180)*/, prop.Position + new Vector3((Vector2)prop.PixelSize, 1)/ 2);
        PixelSize = size;
        Vars = variants;
        Colored = colored ? 1 : 0;
        SmoothShading = smoothShading;
        ContourExponent = contourExponent;
        HighlightMin = highlightMin;
        ShadowMin = shadowMin;
        DepthHighlightExponent = depthHighlightExponent;
        UseRainPalette = prop.Scene.Rain ? 1 : 0;
    }
}

// Used in decals and antimatter
[StructLayout(LayoutKind.Sequential)]
public struct RWBasicPropRenderUniform
{
    public readonly float StartingLayer;
    public readonly float LayerCount;
    public readonly Vector2 RenderableTexSize;

    public readonly Matrix4x4 Rotation;
    public readonly Vector2 PixelSize;
    public readonly int Vars;
    public readonly int UseRainPalette;

    public RWBasicPropRenderUniform(PropObject prop, Vector2 size, int variants)
    {
        StartingLayer = prop.Position.Z; // 4
        LayerCount = prop.LayerCount; // 4
        RenderableTexSize = new Vector2(prop.CachedTexture.Width, prop.CachedTexture.Height); // 8
        // 16 bytes

        Rotation = Matrix4x4.CreateRotationZ(prop.Rotation/* * (float.Pi / 180)*/, prop.Position + new Vector3((Vector2)prop.PixelSize, 1)/ 2);
        PixelSize = size;
        Vars = variants;
        UseRainPalette = prop.Scene.Rain ? 1 : 0;
    }
}