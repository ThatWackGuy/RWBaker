using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace RWBaker.Rendering;

public struct RenderDescription : IDisposable
{
    public readonly Vertex[] LockedVertices;
    public readonly ushort[] LockedIndices;
    public readonly Matrix4x4 MeshMatrix;

    public readonly ResourceSet? TextureSet;

    public readonly int UniformSizeInBytes;
    public readonly uint UniformSizeForBuffer;
    public readonly IntPtr UniformPtr;

    public readonly bool UseRemoval;
    public readonly bool UseLighting;
    public readonly bool UseShaded;

    public readonly ResourceLayout[] Layouts;
    public readonly Shader[] Shaders;

    public readonly RenderPass[] PassesBeforeEverything;
    public readonly RenderPass[] PassesAfterEverything;

    public RenderDescription(Mesh mesh, Vector3 position, Matrix4x4 rotate, ResourceSet? textureSet, object uniformStruct, bool useRemoval, bool useLighting, bool useShaded, ResourceLayout[] layouts, Shader[] shaders, RenderPass[] passesBeforeEverything, RenderPass[] passesAfterEverything)
    {
        lock (mesh)
        {
            LockedVertices = mesh.Vertices;
            LockedIndices = mesh.Indices;
        }

        MeshMatrix = Matrix4x4.CreateWorld(position, -Vector3.UnitZ, Vector3.UnitY) * rotate;

        TextureSet = textureSet;

        UniformSizeInBytes = Marshal.SizeOf(uniformStruct);
        UniformSizeForBuffer = 16 * (uint)float.Ceiling(UniformSizeInBytes / 16f);

        UniformPtr = Marshal.AllocHGlobal(UniformSizeInBytes);
        Marshal.StructureToPtr(uniformStruct, UniformPtr, false);

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
        Marshal.FreeHGlobal(UniformPtr);
        TextureSet?.Dispose();
    }
}