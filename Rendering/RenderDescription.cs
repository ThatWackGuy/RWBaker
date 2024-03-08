using System;
using System.Runtime.InteropServices;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Rendering;

public struct RenderDescription : IDisposable
{
    public readonly RWVertexData[] Vertices;
    public readonly ushort[] Indices;

    public readonly ResourceSet? TextureSet;
    public readonly bool HasTextureSet;

    public readonly int ObjectSizeInBytes;
    public readonly uint ObjectSizeForBuffer;
    public readonly IntPtr ObjectData;

    public readonly Pipeline Pipeline;

    public RenderDescription(RWVertexData[] vertices, ushort[] indices, IRenderable renderable, Scene scene)
    {
        Vertices = vertices;
        Indices = indices;

        HasTextureSet = renderable.GetTextureSet(scene, out ResourceSet? textureSet);
        TextureSet = textureSet;

        object data = renderable.CreateObjectData();
        ObjectSizeInBytes = Marshal.SizeOf(data);
        ObjectSizeForBuffer = 16 * (uint)float.Ceiling(ObjectSizeInBytes / 16f);

        ObjectData = Marshal.AllocHGlobal(ObjectSizeInBytes);
        Marshal.StructureToPtr(data, ObjectData, false);

        Pipeline = renderable.GetPipeline();
    }

    public void Dispose()
    {
        // Pipeline belongs to the renderable, it must dispose the pipeline itself!
        Marshal.FreeHGlobal(ObjectData);
        if (HasTextureSet) TextureSet!.Dispose();
    }
}