using System;
using System.Data;
using System.Numerics;

namespace RWBaker.Rendering;

public class Mesh
{
    public static readonly Mesh Empty = new() { IsStaticEmpty = true };

    public Vertex[] Vertices = [];
    public ushort[] Indices = [];

    public bool IsStaticEmpty { get; init; }
    public bool IsEmpty => Vertices.Length == 0 && Indices.Length == 0;

    private bool _nextMergeAllocated;
    private int _originalVtxSize;
    private int _originalIdxSize;

    /// <summary>
    /// Resizes the arrays to fit given vertAmount and indexAmount
    /// </summary>
    /// <param name="vertAmount">How many vertices will be added</param>
    /// <param name="indexAmount">How many vertices will be added</param>
    /// <param name="perfectAllocForNextMerge">Whether added amount is equal to or more than the required size</param>
    /// <remarks>
    /// Don't forget to call <see cref="AllocatedMergeOver"/> if perfectAllocForNextMerge is true!
    /// </remarks>
    public void ReadyMerge(int vertAmount, int indexAmount, bool perfectAllocForNextMerge = false)
    {
        if (IsStaticEmpty) throw new ReadOnlyException("Static Empty Mesh cannot be changed!");

        _originalVtxSize = Vertices.Length;
        _originalIdxSize = Indices.Length;

        Array.Resize(ref Vertices, Vertices.Length + vertAmount);
        Array.Resize(ref Indices, Indices.Length + indexAmount);

        _nextMergeAllocated = perfectAllocForNextMerge;
    }

    /// <summary>
    /// see: <see cref="ReadyMerge"/>
    /// </summary>
    public void AllocatedMergeOver()
    {
        if (IsStaticEmpty) throw new ReadOnlyException("Static Empty Mesh cannot be changed!");

        _originalVtxSize = Vertices.Length;
        _originalIdxSize = Indices.Length;

        _nextMergeAllocated = false;
    }

    public void MergeMesh(Mesh merge) => MergeVertices(merge.Vertices, merge.Indices);

    public void MergeMesh(Mesh merge, Vector3 offset) => MergeVertices(merge.Vertices, merge.Indices, offset);

    public void MergeVertices(Span<Vertex> vertices, Span<ushort> indexOffsets)
    {
        if (IsStaticEmpty) throw new ReadOnlyException("Static Empty Mesh cannot be changed!");

        int originalVtxLength = _nextMergeAllocated ? _originalVtxSize : Vertices.Length;
        int originalIdxLength = _nextMergeAllocated ? _originalIdxSize : Indices.Length;

        if (Vertices.Length < originalVtxLength + vertices.Length) Array.Resize(ref Vertices, originalVtxLength + vertices.Length);
        if (Indices.Length < originalIdxLength + indexOffsets.Length) Array.Resize(ref Indices, originalIdxLength + indexOffsets.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vertices[originalVtxLength + i] = vertices[i];
        }

        for (int i = 0; i < indexOffsets.Length; i++)
        {
            Indices[originalIdxLength + i] = (ushort)(originalVtxLength + indexOffsets[i]);
        }

        _originalVtxSize += vertices.Length;
        _originalIdxSize += indexOffsets.Length;
    }

    public void MergeVertices(Span<Vertex> vertices, Span<ushort> indexOffsets, Vector3 offset)
    {
        if (IsStaticEmpty) throw new ReadOnlyException("Static Empty Mesh cannot be changed!");

        int originalVtxLength = Vertices.Length;
        int originalIdxLength = Indices.Length;

        if (Vertices.Length < originalVtxLength + vertices.Length) Array.Resize(ref Vertices, originalVtxLength + vertices.Length);
        if (Indices.Length < originalIdxLength + indexOffsets.Length) Array.Resize(ref Indices, originalIdxLength + indexOffsets.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vertex vert = vertices[i];
            Vertices[originalVtxLength + i] = new Vertex(vert.Position + offset, vert.TexturePos, vert.Color);
        }

        for (int i = 0; i < indexOffsets.Length; i++)
        {
            Indices[originalIdxLength + i] = (ushort)(originalVtxLength + indexOffsets[i]);
        }

        _originalVtxSize += vertices.Length;
        _originalIdxSize += indexOffsets.Length;
    }

    public void MergeQuad(Span<Vertex> vertices) => MergeVertices(vertices, [0, 1, 2, 2, 3, 0]);

    public void MergeQuad(Span<Vertex> vertices, Vector3 offset) => MergeVertices(vertices, [0, 1, 2, 2, 3, 0], offset);

    [Obsolete("SAVING / LOADING DOES NOT WORK! DO NOT USE", true)]
    public void MergeBytes(byte[] bytes)
    {
        byte[] sizeBuffer = new byte[4];

        Buffer.BlockCopy(bytes, 0, sizeBuffer, 0, 4);
        int vtxSize = BitConverter.ToInt32(sizeBuffer);

        Buffer.BlockCopy(bytes, 4, sizeBuffer, 0, 4);
        int idxSize = BitConverter.ToInt32(sizeBuffer);

        Vertex[] vertices = new Vertex[vtxSize];
        ushort[] indices = new ushort[idxSize];

        Buffer.BlockCopy(bytes, 8, vertices, 0, vtxSize * 36);
        Buffer.BlockCopy(bytes, 8 + vtxSize * 36, indices, 0, idxSize * 2);

        ReadyMerge(vtxSize, idxSize, true);
        MergeVertices(vertices, indices);
        AllocatedMergeOver();
    }

    public void Clear()
    {
        Vertices = [];
        Indices = [];

        _nextMergeAllocated = false;
        _originalVtxSize = 0;
        _originalIdxSize = 0;
    }

    [Obsolete("SAVING / LOADING DOES NOT WORK! DO NOT USE", true)]
    public unsafe byte[] AsBytes()
    {
        if (IsStaticEmpty) throw new ReadOnlyException("What are you doing?");

        /*
         * VALUE        : OFFSET + SIZE
         * --------------------------------
         * VerticesSize : 0 + 4
         * IndicesSize  : 4 + 4
         * Vertices     : 8 + VLENGTH
         * Indices      : (8 + VLENGTH) + ILENGTH
         */

        int vLength = Vertices.Length * 36;
        int iLength = Vertices.Length * 2;
        byte[] bytes = new byte[8 + vLength + iLength];

        byte[] vtxSize = BitConverter.GetBytes(Vertices.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(vtxSize);

        byte[] idxSize = BitConverter.GetBytes(Indices.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(idxSize);

        Buffer.BlockCopy(vtxSize, 0, bytes, 0, 4);
        Buffer.BlockCopy(idxSize, 0, bytes, 4, 4);

        fixed(void* vtx = &Vertices[0], dst = &bytes[8]) Buffer.MemoryCopy(vtx, dst, vLength + iLength, vLength);

        Buffer.BlockCopy(Indices, 0, bytes, 8 + vLength, iLength);

        return bytes;
    }
}