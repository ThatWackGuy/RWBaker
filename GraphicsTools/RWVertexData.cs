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
        Position = new Vector3(position, 0.1f);
        TexturePos = Vector2.Zero;
        Color = RgbaFloat.Clear;
    }

    public static IEnumerable<RWVertexData> TestQuadVertices(float mult)
    {
        yield return new RWVertexData(new Vector2(-1, +1) * mult);
        yield return new RWVertexData(new Vector2(+1, +1) * mult);
        yield return new RWVertexData(new Vector2(+1, -1) * mult);
        yield return new RWVertexData(new Vector2(-1, -1) * mult);
    }
    
    public static IEnumerable<ushort> TestQuadIndices()
    {
        yield return 0;
        yield return 1;
        yield return 2;
        yield return 2;
        yield return 3;
        yield return 0;
    }
}