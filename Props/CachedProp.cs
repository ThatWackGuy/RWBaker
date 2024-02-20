using System;
using System.IO;
using System.Linq;
using System.Numerics;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Props;

public class CachedProp : IRWRenderable, IDisposable
{
    public readonly string Name;
    public readonly string ProperName;
    private readonly int[] renderRepeatLayers;
    public readonly Vector2Int PixelSize;

    private readonly IProp.UniformConstructor _uniformConstructor;
    private readonly IProp.TexPosCalculator _texPosCalculator;
    private readonly ShaderSetDescription _shaderSet;

    public readonly int Variants;
    private int _renderVariation;
    public int RenderVariation
    {
        get => _renderVariation;

        set => _renderVariation = int.Clamp(value, 0, Variants - 1);
    }

    public bool UseRainPalette;

    public readonly int Depth;
    private int _renderSubLayer;
    public int RenderSubLayer
    {
        get => _renderSubLayer;

        set => _renderSubLayer = int.Clamp(value, 0, 30 - Depth);
    }

    public readonly Texture CachedTexture;

    public CachedProp()
    {
        Name = "";
        ProperName = "";

        _uniformConstructor = _ => throw new Exception();

        renderRepeatLayers = new[] { 1 };

        PixelSize = Vector2Int.One;

        Variants = 1;

        Depth = 1;

        CachedTexture = GuiManager.MissingTex.Texture;
    }

    public CachedProp(RWObjectManager manager, IProp cache)
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
        _shaderSet = cache.ShaderSetDescription();

        renderRepeatLayers = cache.RepeatLayers();

        if (renderRepeatLayers.Length > 30)
        {
            renderRepeatLayers = renderRepeatLayers[..30];
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

        PixelSize = cache.Size();

        Variants = cache.Variants();

        Depth = renderRepeatLayers.Sum();

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
            Vector2 texPos = _texPosCalculator(RenderVariation, imgLayer);
            Vector2 texSize = (Vector2)PixelSize;

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

    public DeviceBuffer CreateObjectData(RWScene scene) => _uniformConstructor(this);

    public Vector2Int GetRenderSize(RWScene scene) => PixelSize + (LayerCount() - 1) * Vector2.Abs(scene.ObjectOffset);

    public Vector2 GetTextureSize() => new(CachedTexture.Width, CachedTexture.Height);

    public ShaderSetDescription GetShaderSetDescription() => _shaderSet;

    public int LayerCount() => renderRepeatLayers.Sum();
    public int Layer() => _renderSubLayer;

    public bool GetTextureSet(RWScene scene, out ResourceSet textureSet)
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