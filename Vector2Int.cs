using System.Numerics;
using System.Text.Json.Serialization;

namespace RWBaker;

// TODO: THIS THING IS AWFUL
public struct Vector2Int
{
    [JsonInclude]
    public int X;

    [JsonInclude]
    public int Y;

    [JsonIgnore]
    public static Vector2Int Zero = new(0, 0);

    [JsonIgnore]
    public static Vector2Int One = new(1, 1);

    public Vector2Int(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Vector2Int Abs()
    {
        return new Vector2Int(int.Abs(X), int.Abs(Y));
    }

    public static Vector2Int Max(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(int.Max(a.X, b.X), int.Max(a.Y, b.Y));
    }

    public static explicit operator Vector2Int(Vector2 v) => new((int)v.X, (int)v.Y);

    public static explicit operator Vector2(Vector2Int v) => new(v.X, v.Y);

    // V2I - V2I
    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2Int operator *(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X * b.X, a.Y * b.Y);
    }

    public static Vector2Int operator /(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X / b.X, a.Y / b.Y);
    }

    // V2I - V2
    public static Vector2Int operator +(Vector2Int a, Vector2 b)
    {
        return new Vector2Int((int)(a.X + b.X), (int)(a.Y + b.Y));
    }

    public static Vector2Int operator -(Vector2Int a, Vector2 b)
    {
        return new Vector2Int((int)(a.X - b.X), (int)(a.Y - b.Y));
    }

    public static Vector2Int operator *(Vector2Int a, Vector2 b)
    {
        return new Vector2Int((int)(a.X * b.X), (int)(a.Y * b.Y));
    }

    public static Vector2Int operator /(Vector2Int a, Vector2 b)
    {
        return new Vector2Int((int)(a.X / b.X), (int)(a.Y / b.Y));
    }

    // V2 - V2I
    public static Vector2Int operator +(Vector2 a, Vector2Int b)
    {
        return new Vector2Int((int)(a.X + b.X), (int)(a.Y + b.Y));
    }

    public static Vector2Int operator -(Vector2 a, Vector2Int b)
    {
        return new Vector2Int((int)(a.X - b.X), (int)(a.Y - b.Y));
    }

    public static Vector2Int operator *(Vector2 a, Vector2Int b)
    {
        return new Vector2Int((int)(a.X * b.X), (int)(a.Y * b.Y));
    }

    public static Vector2Int operator /(Vector2 a, Vector2Int b)
    {
        return new Vector2Int((int)(a.X / b.X), (int)(a.Y / b.Y));
    }

    // V2I - Int
    public static Vector2Int operator +(Vector2Int a, int b)
    {
        return new Vector2Int(a.X + b, a.Y + b);
    }

    public static Vector2Int operator -(Vector2Int a, int b)
    {
        return new Vector2Int(a.X - b, a.Y - b);
    }

    public static Vector2Int operator *(Vector2Int a, int b)
    {
        return new Vector2Int(a.X * b, a.Y * b);
    }

    public static Vector2Int operator /(Vector2Int a, int b)
    {
        return new Vector2Int(a.X / b, a.Y / b);
    }
}