using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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

    public readonly Mesh Mesh;

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

        Mesh = new Mesh();
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
        Size = new Vector3(OriginalSize * 20 + Vector2.One * cache.BufferTiles * 40, LayerCount);

        Position = new Vector3(-Size.X / 2, -Size.Y / 2 ,0);

        Type = cache.Type;

        Variants = cache.Variants;

        HasSpecs2 = cache.HasSpecs2;

        if (!GuiTexture.TryGetTexture($"_tile{ProperName}", out CachedTexture!))
        {
            using Image<Rgba32> image = Image.Load<Rgba32>(texturePath);

            image.Mutate(ctx =>
            {
                ctx.FixFutileTexture((int)Size.X * Variants, (int)Size.Y * RepeatLayers.Length);
                ctx.ReadyFutileTexture();
            });

            CachedTexture = GuiTexture.Create($"_tile{ProperName}", GuiManager.TextureFromImage(image));
        }

        CachedTexture.Use();

        Mesh = new Mesh();
        BuildLayerMesh();
    }

    public void RenderInspector()
    {
        if (Scene.ActiveCamera == null) return;

        ImGui.DragFloat("X", ref Position.X, 20, 40);
        ImGui.DragFloat("Y", ref Position.Y, 20, 40);

        int z = (int)Position.Z / 10;
        ImGui.SliderInt("Layer", ref z, 0, 2);
        Position.Z = z * 10;

        if (Variants > 1)
        {
            if (ImGui.SliderInt("Variant", ref _renderVariation, 0, Variants - 1)) BuildLayerMesh();
        }
    }

    public RenderDescription GetRenderDescription(Camera camera)
    {
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

        lock (Mesh) return new RenderDescription(
            Mesh, Position, Matrix4x4.Identity,
            textureSet,
            new RWTileRenderUniform(this),
            false, true, true,
            RWUtils.RWResourceLayout, RWUtils.TileShaders,
            [], []
        );
    }

    private void BuildLayerMesh()
    {
        Mesh.Clear();

        /*if (File.Exists($"./cache/meshes/{ProperName} VAR{RenderVariation}"))
        {
            Mesh.MergeBytes(File.ReadAllBytes($"./cache/meshes/{ProperName} VAR{RenderVariation}"));
            return;
        }*/

        Mesh.ReadyMerge(LayerCount * 4, LayerCount * 6, true);

        int renderLayer = 0;
        for (int imgLayer = 0; imgLayer < RepeatLayers.Length; imgLayer++)
        {
            if (RepeatLayers[imgLayer] == 0) continue;

            // box tiles calculate texture coords in-shader
            Vector2 texPos = Type == TileType.Box ? Vector2.Zero : new Vector2(Size.X * RenderVariation, 1 + imgLayer * Size.Y);

            for (int repeat = 0; repeat < RepeatLayers[imgLayer]; repeat++)
            {
                Mesh.MergeQuad([
                    new Vertex(
                        new Vector3(0, Size.Y, renderLayer),
                        texPos,
                        RgbaFloat.Clear
                    ), // Top Left

                    new Vertex(
                        new Vector3(Size.X, Size.Y, renderLayer),
                        texPos with { X = texPos.X + Size.X },
                        RgbaFloat.Clear
                    ), // Top Right

                    new Vertex(
                        new Vector3(Size.X, 0, renderLayer),
                        new Vector2(texPos.X + Size.X, texPos.Y + Size.Y),
                        RgbaFloat.Clear
                    ), // Bottom Right

                    new Vertex(
                        new Vector3(0, 0, renderLayer),
                        texPos with { Y = texPos.Y + Size.Y },
                        RgbaFloat.Clear
                    ) // Bottom Left
                ]);

                renderLayer++;
            }
        }

        Mesh.AllocatedMergeOver();

        /*Image<Rgba32> image = CachedTexture.ToImage(true);

        Parallel.For(0, RepeatLayers.Length, ly =>
        {
            Mesh extruded = image.ExtrudeEdgesAsMesh(new Rectangle(RenderVariation * (int)Size.X, 1 + ly * (int)Size.Y, (int)Size.X, (int)Size.Y), ly);

            for (int repeated = 0; repeated < RepeatLayers[ly]; repeated++)
            {
                lock (Mesh) Mesh.MergeMesh(extruded, Vector3.UnitZ * repeated);
            }
        });

        GuiManager.PushNotification(new ImNotify(ImNotifyType.Success, $"Successfully built mesh {ProperName} VAR{RenderVariation}"));*/

        /*Directory.CreateDirectory("./cache/meshes");
        using FileStream file = File.Create($"./cache/meshes/{ProperName} VAR{RenderVariation}");

        lock (Mesh) file.Write(Mesh.AsBytes());
        file.Close();*/
    }

    public Vector2Int GetRenderSize(Camera camera) => new Vector2Int((int)Size.X, (int)Size.Y) + (LayerCount - 1) * Vector2.One; // TODO: FIX

    public void Dispose()
    {
        CachedTexture.Release();

        GC.SuppressFinalize(this);
    }
}