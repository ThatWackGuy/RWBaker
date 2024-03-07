using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Tiles;

public class TileObject : SceneObject, IRenderable, IInspectable, IDisposable
{
    public readonly string Name;
    public readonly string ProperName;
    public readonly int[] RenderRepeatLayers;
    public readonly int LayerCount;
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

    public readonly bool HasSpecs2;

    public readonly Pipeline Pipeline;
    public readonly Texture CachedTexture;

    // Default when there are no tiles
    public TileObject() : base(null, "")
    {
        Name = "";
        ProperName = "";

        RenderRepeatLayers = new[] { 1 };
        LayerCount = 1;

        Size = Vector2Int.One;
        BufferTiles = 0;
        PixelSize = Vector2Int.One;

        Type = TileType.VoxelStruct;

        Variants = 1;

        HasSpecs2 = false;

        Pipeline = null!;

        CachedTexture = GuiManager.MissingTex.Texture;
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

        RenderRepeatLayers = cache.RepeatLayers;

        if (cache.RepeatLayers.Length > 30)
        {
            RenderRepeatLayers = cache.RepeatLayers[..30];
        }

        if (RenderRepeatLayers.Sum() >= 30)
        {
            int check = 0;
            for (int i = 0; i < RenderRepeatLayers.Length; i++)
            {
                int repeat = RenderRepeatLayers[i];

                if (check + repeat > 30)
                {
                    RenderRepeatLayers = RenderRepeatLayers[..i];
                    Array.Resize(ref RenderRepeatLayers, i + 1);
                    RenderRepeatLayers[i] = 30 - check;
                    break;
                }

                check += repeat;
            }
        }

        LayerCount = RenderRepeatLayers.Sum();

        Size = cache.Size;
        BufferTiles = cache.BufferTiles;
        PixelSize = cache.Size * 20 + cache.BufferTiles * 40;

        Type = cache.Type;

        Variants = cache.Variants;

        HasSpecs2 = cache.HasSpecs2;

        Pipeline = GuiManager.ResourceFactory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                RWUtils.TileRendererShaderSet,
                RWUtils.RWResourceLayout,
                scene.ObjectFramebuffer.OutputDescription
            )
        );

        CachedTexture = GuiManager.TextureFromImage(texturePath);
    }

    public void RenderInspector()
    {
        int x = (int)Position.X;
        int y = (int)Position.Y;
        int z = (int)Position.Z;
        ImGui.SliderInt("X", ref x, 0, (int)Scene.Width);
        ImGui.SliderInt("Y", ref y, 0, (int)Scene.Height);
        ImGui.SliderInt("Layer", ref z, 0, 2);

        Position.X = x;
        Position.Y = y;
        Position.Z = z;
    }

    public RenderDescription GetRenderDescription(Scene scene)
    {
        var vertices = new RWVertexData[LayerCount * 4];
        ushort[] indices = new ushort[LayerCount * 6];

        int vertIndex = 0;
        int indexIndex = 0;
        int renderLayer = 0;
        for (int imgLayer = 0; imgLayer < RenderRepeatLayers.Length; imgLayer++)
        {
            if (RenderRepeatLayers[imgLayer] == 0) continue;

            // Vector2 vertPos = new Vector2(float.Max(per.X * -1, 0), float.Max(per.Y * -1, 0)) * (renderRepeatLayers.Length - 1) + per * imgLayer
            Vector3 pos = Position with { Z = Position.Z * 10 };
            Vector2 texPos = Type == TileType.Box ? Vector2.Zero : new Vector2(PixelSize.X * RenderVariation,  1 + imgLayer * PixelSize.Y);
            Vector2 texSize = (Vector2)PixelSize;

            // box texture pos is calculated in-shader
            // so assignment here means nothing

            for (int repeat = 0; repeat < RenderRepeatLayers[imgLayer]; repeat++)
            {
                vertices[vertIndex] = new RWVertexData(
                    pos + new Vector3(Vector2.Zero, renderLayer),
                    texPos,
                    RgbaFloat.Clear
                ); // Top Left

                vertices[vertIndex + 1] = new RWVertexData(
                    pos + new Vector3(PixelSize.X, 0, renderLayer),
                    texPos with { X = texPos.X + texSize.X },
                    RgbaFloat.Clear
                ); // Top Right

                vertices[vertIndex + 2] = new RWVertexData(
                    pos + new Vector3(PixelSize.X, PixelSize.Y, renderLayer),
                    texPos + texSize,
                    RgbaFloat.Clear
                ); // Bottom Right

                vertices[vertIndex + 3] = new RWVertexData(
                    pos + new Vector3(0, PixelSize.Y, renderLayer),
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

        return new RenderDescription(vertices, indices, this, scene);
    }

    public DeviceBuffer CreateObjectData()
    {
        DeviceBuffer buffer = GuiManager.ResourceFactory.CreateStructBuffer<RWTileRenderUniform>();

        GuiManager.GraphicsDevice.UpdateBuffer(buffer, 0, new RWTileRenderUniform(this));

        return buffer;
    }

    public Pipeline GetPipeline() => Pipeline;

    public Vector2Int GetRenderSize(Scene scene) => PixelSize + (LayerCount - 1) * Vector2.Abs(scene.ObjectOffset);

    public bool GetTextureSet(Scene scene, [MaybeNullWhen(false)] out ResourceSet textureSet)
    {
        textureSet = GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectTextureLayout,
                CachedTexture,
                scene.PaletteManager.CurrentPalette.DisplayTex.Texture,
                scene.PaletteManager.EffectColors.Texture,
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