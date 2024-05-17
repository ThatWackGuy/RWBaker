namespace RWBaker.Rendering;

/// <summary> Represents a <see cref="SceneObject"/> that can be drawn by a <see cref="Camera"/> </summary>
public interface IRenderable
{
    public RenderDescription GetRenderDescription(Camera camera);

    public Vector2Int GetRenderSize(Camera camera);
}