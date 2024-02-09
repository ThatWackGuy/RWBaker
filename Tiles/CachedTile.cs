using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Tiles;

public class CachedTile : IRWRenderable, IDisposable
{
    public readonly string Name;
    public readonly string ProperName;
    private readonly int[] renderRepeatLayers;
    public readonly Vector2Int Size;
    public readonly int BufferTiles;
    public readonly Vector2Int PixelSize;
    public readonly TileType Type;

    public readonly int Variants;
    private int _renderVariation;
    public int RenderVariation
    {
        get => _renderVariation;

        set => _renderVariation = int.Clamp(value, 0, Variants - 1);
    }

    public bool UseRainPalette;

    public readonly bool HasSpecs2;
    private int _renderLayer;
    public int RenderLayer
    {
        get => _renderLayer;

        set => _renderLayer = int.Clamp(value, 0, HasSpecs2 ? 1 : 2);
    }

    public readonly Texture CachedTexture;

    // Default when there are no tiles
    public CachedTile()
    {
        Name = "";
        ProperName = "";

        renderRepeatLayers = new[] { 1 };

        Size = Vector2Int.One;
        BufferTiles = 0;
        PixelSize = Vector2Int.One;

        Type = TileType.VoxelStruct;

        Variants = 1;

        HasSpecs2 = false;

        CachedTexture = GuiManager.MissingTex.Texture;
    }

    public CachedTile(RWObjectManager manager, Tile cache)
    {
        string texturePath = Path.Combine(manager.GraphicsDir, $"{cache.Name}.png");

        if (!File.Exists(texturePath))
        {
            throw new FileNotFoundException("Please check the names or if the file has been deleted!");
        }

        Name = cache.Name;
        ProperName = cache.ProperName;

        renderRepeatLayers = cache.RepeatLayers;

        if (cache.RepeatLayers.Length > 30)
        {
            renderRepeatLayers = cache.RepeatLayers[..30];
        }

        if (renderRepeatLayers.Sum() >= 30)
        {
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

        Size = cache.Size;
        BufferTiles = cache.BufferTiles;
        PixelSize = cache.Size * 20 + cache.BufferTiles * 40;

        Type = cache.Type;

        Variants = cache.Variants;

        HasSpecs2 = cache.HasSpecs2;

        CachedTexture = GuiManager.TextureFromImage(texturePath);
    }

    public RWRenderDescription GetSceneInfo(RWScene scene)
    {
        int layerCount = renderRepeatLayers.Sum();
        var vertices = new RWVertexData[layerCount * 4];
        ushort[] indices = new ushort[layerCount * 6];

        int vertIndex = 0;
        int indexIndex = 0;
        int renderLayer = 0;
        for (int imgLayer = 0; imgLayer < renderRepeatLayers.Length; imgLayer++)
        {
            if (renderRepeatLayers[imgLayer] == 0) continue;

            // Vector2 vertPos = new Vector2(float.Max(per.X * -1, 0), float.Max(per.Y * -1, 0)) * (renderRepeatLayers.Length - 1) + per * imgLayer
            Vector2 texPos = Type == TileType.Box ? Vector2.Zero : new Vector2(PixelSize.X * RenderVariation,  1 + imgLayer * PixelSize.Y);
            Vector2 texSize = (Vector2)PixelSize;

            // box texture pos is calculated in-shader
            // so assignment here means nothing

            for (int repeat = 0; repeat < renderRepeatLayers[imgLayer]; repeat++)
            {
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
                renderLayer++;
            }
        }

        return new RWRenderDescription(vertices, indices, this, scene);
    }

    public DeviceBuffer CreateObjectData(RWScene scene)
    {
        DeviceBuffer buffer = GuiManager.ResourceFactory.CreateStructBuffer<RWTileRenderUniform>();

        GuiManager.GraphicsDevice.UpdateBuffer(buffer, 0, new RWTileRenderUniform(this));

        return buffer;
    }

    public ShaderSetDescription GetShaderSetDescription() => RWUtils.TileRendererShaderSet;

    public int LayerCount() => renderRepeatLayers.Sum();
    public int Layer() => _renderLayer;

    public Vector2Int GetRenderSize(RWScene scene) => PixelSize + (LayerCount() - 1) * Vector2.Abs(scene.ObjectOffset);

    public Vector2 GetTextureSize() => new(CachedTexture.Width, CachedTexture.Height);

    public bool GetTextureSet(RWScene scene, [MaybeNullWhen(false)] out ResourceSet textureSet)
    {
        textureSet = GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectTextureLayout,
                CachedTexture,
                scene.Palettes.CurrentPalette.DisplayTex.Texture,
                scene.Palettes.EffectColors.Texture,
                scene.ShadowRender.Texture
            )
        );

        return true;
    }

    public void Dispose()
    {
        CachedTexture.Dispose();
        GC.SuppressFinalize(this);
    }
}