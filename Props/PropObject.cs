using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Props;

public class PropObject : SceneObject, IRenderable, IInspectable, IDisposable
{
    public readonly string Name;
    public readonly string ProperName;
    public readonly int[] RenderRepeatLayers;
    public readonly int LayerCount;
    public readonly Vector2Int PixelSize;
    public float Rotation; // Rotation in radians

    private readonly IProp.UniformConstructor _uniformConstructor;
    private readonly IProp.TexPosCalculator _texPosCalculator;
    private readonly Pipeline _pipeline;

    public readonly int Variants;
    private int _renderVariation;
    public int RenderVariation
    {
        get => _renderVariation;

        set => _renderVariation = int.Clamp(value, 0, Variants - 1);
    }

    public readonly int Depth;

    public readonly Texture CachedTexture;

    public PropObject() : base(null, "")
    {
        Name = "";
        ProperName = "";

        _uniformConstructor = _ => throw new Exception();
        _texPosCalculator = (_, _) => throw new Exception();

        RenderRepeatLayers = new[] { 1 };
        LayerCount = 1;

        PixelSize = Vector2Int.One;

        Variants = 1;

        Depth = 1;

        _pipeline = null!;

        CachedTexture = GuiManager.MissingTex.Texture;
    }

    public PropObject(Scene scene, RWObjectManager manager, IProp cache) : base(scene, cache.ProperName())
    {
        Name = cache.Name();
        ProperName = cache.ProperName();

        string texturePath = Path.Combine(manager.PropsDir, $"{Name}.png");

        if (!File.Exists(texturePath))
        {
            throw new FileNotFoundException("Please check the names or if the file has been deleted!");
        }

        _uniformConstructor = cache.GetUniform();
        _texPosCalculator = cache.GetTexPos();

        _pipeline = GuiManager.ResourceFactory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                cache.ShaderSetDescription(),
                RWUtils.RWResourceLayout,
                scene.ObjectFramebuffer.OutputDescription
            )
        );

        RenderRepeatLayers = cache.RepeatLayers();

        if (RenderRepeatLayers.Length > 30)
        {
            RenderRepeatLayers = RenderRepeatLayers[..30];
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

        PixelSize = cache.Size();

        Variants = cache.Variants();

        Depth = RenderRepeatLayers.Sum();

        CachedTexture = GuiManager.TextureFromImage(texturePath);
    }

    public void RenderInspector()
    {
        int x = (int)Position.X;
        int y = (int)Position.Y;
        int z = (int)Position.Z;
        ImGui.SliderInt("X", ref x, 0, (int)Scene.Width);
        ImGui.SliderInt("Y", ref y, 0, (int)Scene.Height);
        ImGui.SliderInt("Layer", ref z, 0, 29 - Depth);

        ImGui.SliderAngle("Rotate", ref Rotation);

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
            Vector2 texPos = _texPosCalculator(RenderVariation, imgLayer);
            Vector2 texSize = (Vector2)PixelSize;

            for (int repeat = 0; repeat < RenderRepeatLayers[imgLayer]; repeat++)
            {
                vertices[vertIndex] = new RWVertexData(
                    Position + new Vector3(Vector2.Zero, renderLayer),
                    texPos,
                    RgbaFloat.Clear
                ); // Top Left

                vertices[vertIndex + 1] = new RWVertexData(
                    Position + new Vector3(PixelSize.X, 0, renderLayer),
                    texPos with { X = texPos.X + texSize.X },
                    RgbaFloat.Clear
                ); // Top Right

                vertices[vertIndex + 2] = new RWVertexData(
                    Position + new Vector3(PixelSize.X, PixelSize.Y, renderLayer),
                    texPos + texSize,
                    RgbaFloat.Clear
                ); // Bottom Right

                vertices[vertIndex + 3] = new RWVertexData(
                    Position + new Vector3(0, PixelSize.Y, renderLayer),
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

    public DeviceBuffer CreateObjectData() => _uniformConstructor(this);

    public Vector2Int GetRenderSize(Scene scene) => PixelSize + (LayerCount - 1) * Vector2.Abs(scene.ObjectOffset);

    public Pipeline GetPipeline() => _pipeline;

    public bool GetTextureSet(Scene scene, out ResourceSet textureSet)
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