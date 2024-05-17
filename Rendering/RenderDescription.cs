using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace RWBaker.Rendering;

public struct RenderDescription : IDisposable
{
    public readonly string Id;

    public readonly RWVertexData[] Vertices;
    public readonly ushort[] Indices;

    public readonly ResourceSet? TextureSet;

    public readonly int ObjectSizeInBytes;
    public readonly uint ObjectSizeForBuffer;
    public readonly IntPtr ObjectData;

    public readonly bool UseRemoval;
    public readonly bool UseLighting;
    public readonly bool UseShaded;

    public readonly ResourceLayout[] Layouts;
    public readonly Shader[] Shaders;

    public readonly RenderPass[] PassesBeforeEverything;
    public readonly RenderPass[] PassesAfterEverything;

    public RenderDescription(string id, RWVertexData[] vertices, ushort[] indices, ResourceSet? textureSet, object uniformStruct, bool useRemoval, bool useLighting, bool useShaded, ResourceLayout[] layouts, Shader[] shaders, RenderPass[] passesBeforeEverything, RenderPass[] passesAfterEverything)
    {
        Id = id;

        Vertices = vertices;
        Indices = indices;

        TextureSet = textureSet;

        ObjectSizeInBytes = Marshal.SizeOf(uniformStruct);
        ObjectSizeForBuffer = 16 * (uint)float.Ceiling(ObjectSizeInBytes / 16f);

        ObjectData = Marshal.AllocHGlobal(ObjectSizeInBytes);
        Marshal.StructureToPtr(uniformStruct, ObjectData, false);

        UseRemoval = useRemoval;
        UseLighting = useLighting;
        UseShaded = useShaded;

        Layouts = layouts;
        Shaders = shaders;

        PassesBeforeEverything = passesBeforeEverything;
        PassesAfterEverything = passesAfterEverything;
    }

    public void Dispose()
    {
        // Pipeline belongs to the renderable, it must dispose the pipeline itself!
        Marshal.FreeHGlobal(ObjectData);
        TextureSet?.Dispose();
    }
}