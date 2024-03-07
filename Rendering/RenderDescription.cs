using System;
using System.Numerics;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Rendering;

public struct RenderDescription : IDisposable
{
    public readonly RWVertexData[] Vertices;
    public readonly ushort[] Indices;

    public readonly ResourceSet? TextureSet;
    public readonly bool HasTextureSet;

    public readonly DeviceBuffer ObjectDataBuffer;

    public readonly Pipeline Pipeline;

    public RenderDescription(RWVertexData[] vertices, ushort[] indices, IRenderable renderable, Scene scene)
    {
        Vertices = vertices;
        Indices = indices;

        HasTextureSet = renderable.GetTextureSet(scene, out ResourceSet? textureSet);
        TextureSet = textureSet;

        ObjectDataBuffer = renderable.CreateObjectData();

        Pipeline = renderable.GetPipeline();
    }

    public void Dispose()
    {
        Pipeline.Dispose();
        ObjectDataBuffer.Dispose();
        if (HasTextureSet) TextureSet!.Dispose();
    }
}