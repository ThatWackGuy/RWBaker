using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace RWBaker.GraphicsTools;

[StructLayout(LayoutKind.Sequential)]
public struct RWVertexData
{
    public readonly Vector3 Position; // 3 * 4 bytes -> 12
    public readonly Vector2 TexturePos; // 2 * 4 bytes -> 8
    public readonly RgbaFloat Color; // 4 * 4 bytes -> 16

    // 36 BYTES

    public RWVertexData(Vector3 position, Vector2 texturePos, RgbaFloat color)
    {
        Position = position;
        TexturePos = texturePos;
        Color = color;
    }

    public RWVertexData(Vector2 position)
    {
        Position = new Vector3(position, 0);
        TexturePos = Vector2.Zero;
        Color = RgbaFloat.Green;
    }

    public static RWVertexData[] TestQuadVertices(float mult)
    {
        return new[]
        {
            new RWVertexData(new Vector2(-1, +1) * mult),
            new RWVertexData(new Vector2(+1, +1) * mult),
            new RWVertexData(new Vector2(+1, -1) * mult),
            new RWVertexData(new Vector2(-1, -1) * mult)
        };
    }
    
    public static ushort[] TestQuadIndices()
    {
        return new ushort[]
        {
            0,
            1,
            2,
            2,
            3,
            0
        };
    }
}