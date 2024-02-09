using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using RWBaker.Rendering;
using Veldrid;

namespace RWBaker.RWObjects;

public interface IRWRenderable
{
    public RWRenderDescription GetSceneInfo(RWScene scene);

    public DeviceBuffer CreateObjectData(RWScene scene);

    public Vector2Int GetRenderSize(RWScene scene);

    public Vector2 GetTextureSize();

    public ShaderSetDescription GetShaderSetDescription();

    public int LayerCount();

    public int Layer();

    public bool GetTextureSet(RWScene scene, [MaybeNullWhen(false)] out ResourceSet textureSet);
}