using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json.Serialization;
using RWBaker.GraphicsTools;
using Veldrid;

namespace RWBaker.GeneralTools;

public abstract class RWObject
{
    [JsonIgnore] public bool WarningGenerated;
    
    [JsonIgnore] public string Warnings;
    
    protected RWObject()
    {
        WarningGenerated = false;
        Warnings = "";
    }
    
    protected void LogWarning(string warn, ref string log)
    {
        WarningGenerated = true;
        log += warn + "\n";
        Warnings += warn  + "\n";
    }
}

public interface IRWRenderable
{
    public RWRenderDescription GetSceneInfo(RWScene scene);
    
    public DeviceBuffer CreateObjectData(RWScene scene);
    
    public Vector2Int GetRenderSize(RWScene scene);

    public Vector2 GetTextureSize();
    
    public ShaderSetDescription GetShaderSetDescription();

    public int LayerCount();

    public bool GetTextureSet(RWScene scene, [MaybeNullWhen(false)] out ResourceSet textureSet);
}