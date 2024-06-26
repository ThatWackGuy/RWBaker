using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace RWBaker.Rendering;

/// <summary>
/// Raw vertex data.
/// </summary>
/// <remarks>
/// Front face is always evaluated in clockwise rotation
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public readonly Vector3 Position; // 3 * 4 bytes -> 12
    public readonly Vector2 TexturePos; // 2 * 4 bytes -> 8
    public readonly RgbaFloat Color; // 4 * 4 bytes -> 16

    // 36 BYTES

    public Vertex(Vector3 position, Vector2 texturePos, RgbaFloat color)
    {
        Position = position;
        TexturePos = texturePos;
        Color = color;
    }

    public Vertex(float x, float y, float z, Vector2 texturePos, RgbaFloat color)
    {
        Position = new Vector3(x, y, z);
        TexturePos = texturePos;
        Color = color;
    }

    public Vertex(Vector2 pos, float z)
    {
        Position = new Vector3(pos, z);
        TexturePos = Vector2.Zero;
        Color = RgbaFloat.Clear;
    }
}