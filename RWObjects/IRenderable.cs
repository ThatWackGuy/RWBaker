using System.Diagnostics.CodeAnalysis;
using RWBaker.Rendering;
using Veldrid;

namespace RWBaker.RWObjects;

/**
 * Represents a <see cref="SceneObject"/> that can be drawn by a <see cref="Scene"/>
 */
public interface IRenderable
{
    public RenderDescription GetRenderDescription(Scene scene);

    public object CreateObjectData();

    public Vector2Int GetRenderSize(Scene scene);

    public Pipeline GetPipeline();

    public bool GetTextureSet(Scene scene, [MaybeNullWhen(false)] out ResourceSet textureSet);
}