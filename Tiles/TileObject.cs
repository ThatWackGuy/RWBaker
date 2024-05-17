using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;
using Veldrid;

namespace RWBaker.Tiles;

public class TileObject : SceneObject, IRenderable, IInspectable, IDisposable
{
    public readonly string Name;
    public readonly string ProperName;
    public readonly int[] RepeatLayers;
    public readonly int LayerCount;
    public readonly int BufferTiles;
    public readonly Vector2 OriginalSize;
    public readonly TileType Type;

    public readonly int Variants;
    private int _renderVariation;
    public int RenderVariation
    {
        get => _renderVariation;

        set => _renderVariation = int.Clamp(value, 0, Variants - 1);
    }

    public readonly bool HasSpecs2;

    public readonly GuiTexture CachedTexture;

    // Default when there are no tiles
    public TileObject() : base(null!, "")
    {
        Name = "";
        ProperName = "";

        RepeatLayers = new[] { 1 };
        LayerCount = 1;

        BufferTiles = 0;
        OriginalSize = Vector2.One;

        Type = TileType.VoxelStruct;

        Variants = 1;

        HasSpecs2 = false;

        CachedTexture = GuiManager.MissingTex;
    }

    public TileObject(Scene scene, RWObjectManager manager, TileInfo cache) : base(scene, cache.ProperName)
    {
        string texturePath = Path.Combine(manager.GraphicsDir, $"{cache.Name}.png");

        if (!File.Exists(texturePath))
        {
            throw new FileNotFoundException("Please check the names or if the file has been deleted!");
        }

        Name = cache.Name;
        ProperName = cache.ProperName;

        RepeatLayers = cache.RepeatLayers;

        // Limit layers to 30
        if (cache.RepeatLayers.Length > 30)
        {
            RepeatLayers = cache.RepeatLayers[..30];
        }

        // Decreases total layer count to 30 "intelligently" if it exceeds 30
        if (RepeatLayers.Sum() >= 30)
        {
            int check = 0;
            for (int i = 0; i < RepeatLayers.Length; i++)
            {
                int repeat = RepeatLayers[i];

                if (check + repeat > 30)
                {
                    RepeatLayers = RepeatLayers[..i];
                    Array.Resize(ref RepeatLayers, i + 1);
                    RepeatLayers[i] = 30 - check;
                    break;
                }

                check += repeat;
            }
        }

        LayerCount = RepeatLayers.Sum();

        BufferTiles = cache.BufferTiles;
        OriginalSize = (Vector2)cache.Size;
        Size = OriginalSize * 20 + Vector2.One * cache.BufferTiles * 40;

        Type = cache.Type;

        Variants = cache.Variants;

        HasSpecs2 = cache.HasSpecs2;

        if (!GuiTexture.TryGetTexture($"_tile{ProperName}", out CachedTexture!))
        {
            CachedTexture = GuiTexture.Create($"_tile{ProperName}", GuiManager.TextureFromImage(texturePath));
        }

        CachedTexture.Use();
    }

    public void RenderInspector()
    {
        if (Scene.ActiveCamera == null) return;

        ImGui.DragFloat("X", ref Position.X, 20, 40);
        ImGui.DragFloat("Y", ref Position.Y, 20, 40);

        int z = (int)Position.Z;
        ImGui.SliderInt("Layer", ref z, 0, 2);
        Position.Z = z;

        if (Variants > 1)
        {
            ImGui.SliderInt("Variant", ref _renderVariation, 0, Variants - 1);
        }
    }

    public RenderDescription GetRenderDescription(Camera camera)
    {
        Vector3 pos = Position with { Z = Position.Z * 10 };
        Vector2 centredSize = Size / 2;

        var vertices = new RWVertexData[LayerCount * 4];
        ushort[] indices = new ushort[LayerCount * 6];

        int vertIndex = 0;
        int indexIndex = 0;
        int renderLayer = 0;
        for (int imgLayer = 0; imgLayer < RepeatLayers.Length; imgLayer++)
        {
            if (RepeatLayers[imgLayer] == 0) continue;

            // box tile texture pos is calculated in-shader so assignment here means nothing
            Vector2 texPos = Type == TileType.Box ? Vector2.Zero : new Vector2(Size.X * RenderVariation, 1 + imgLayer * Size.Y);

            for (int repeat = 0; repeat < RepeatLayers[imgLayer]; repeat++)
            {
                vertices[vertIndex] = new RWVertexData(
                    pos + new Vector3(-centredSize.X, centredSize.Y, renderLayer),
                    texPos,
                    RgbaFloat.Clear
                ); // Top Left

                vertices[vertIndex + 1] = new RWVertexData(
                    pos + new Vector3(centredSize.X, centredSize.Y, renderLayer),
                    texPos with { X = texPos.X + Size.X },
                    RgbaFloat.Clear
                ); // Top Right

                vertices[vertIndex + 2] = new RWVertexData(
                    pos + new Vector3(centredSize.X, -centredSize.Y, renderLayer),
                    texPos + Size,
                    RgbaFloat.Clear
                ); // Bottom Right

                vertices[vertIndex + 3] = new RWVertexData(
                    pos + new Vector3(-centredSize.X, -centredSize.Y, renderLayer),
                    texPos with { Y = texPos.Y + Size.Y },
                    RgbaFloat.Clear
                ); // Bottom Left

                // 0 1 2, 2 3 0 for each quad
                indices[indexIndex++] = (ushort) vertIndex;      // 0
                indices[indexIndex++] = (ushort)(vertIndex + 1); // 1
                indices[indexIndex++] = (ushort)(vertIndex + 2); // 2
                indices[indexIndex++] = (ushort)(vertIndex + 2); // 2
                indices[indexIndex++] = (ushort)(vertIndex + 3); // 3
                indices[indexIndex++] = (ushort) vertIndex;      // 0

                vertIndex += 4;
                renderLayer++;
            }
        }

        ResourceSet textureSet = GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectTextureLayout,
                CachedTexture.Texture,
                Scene.PaletteManager.CurrentPalette.DisplayTex.Texture,
                Scene.PaletteManager.EffectColors.Texture,
                camera.LightingPass.DepthTexture,
                camera.RemovalPass.RenderTexture.Texture
            )
        );

        return new RenderDescription(
            "tile",
            vertices,
            indices,
            textureSet,
            new RWTileRenderUniform(this),
            false, true, true,
            RWUtils.RWResourceLayout, RWUtils.TileShaders,
            [], []
        );
    }

    public Vector2Int GetRenderSize(Camera camera) => (Vector2Int)Size + (LayerCount - 1) * Vector2.One; // TODO: FIX

    public void Dispose()
    {
        CachedTexture.Release();

        GC.SuppressFinalize(this);
    }
}