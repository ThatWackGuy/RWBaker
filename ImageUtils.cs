using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RWBaker.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;
using Point = SixLabors.ImageSharp.Point;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace RWBaker;

internal enum EdgeExtruderSide
{
    Top,
    Right,
    Bottom,
    Left,
}

internal class LREdgePoint(Point px, bool isEnd)
{
    public Point Px = px;
    public readonly bool IsEnd = isEnd;
    public bool Consumed;
}

internal class ContinuousEdge
{
    public readonly EdgeExtruderSide Side;

    public Point Start;
    public Point End;

    public readonly float Depth;
    public readonly float DepthExtend;

    public ContinuousEdge(Point start, EdgeExtruderSide side, float depth)
    {
        Side = side;

        Start = start;
        End = new Point(
            start.X + (Side is EdgeExtruderSide.Top or EdgeExtruderSide.Bottom ? 1 : 0),
            start.Y + (Side is EdgeExtruderSide.Right or EdgeExtruderSide.Left ? 1 : 0)
        );

        Depth = depth;
        DepthExtend = Depth + 1; // TODO: CUSTOM DEPTH EXTENSION FOR TIGHTER LAYERS
    }

    public Span<Vertex> ToVertices()
    {
        Vector2 S = Start;
        Vector2 E = End;

        return Side switch
        {
            EdgeExtruderSide.Top =>    new Vertex[] {new(E, Depth), new(E, DepthExtend), new(S, DepthExtend), new(S, Depth)},
            EdgeExtruderSide.Right =>  new Vertex[] {new(E, DepthExtend), new(S, DepthExtend), new(S, Depth), new(E, Depth)},
            EdgeExtruderSide.Bottom => new Vertex[] {new(E, DepthExtend), new(E, Depth), new(S, Depth), new(S, DepthExtend)},
            EdgeExtruderSide.Left =>   new Vertex[] {new(E, Depth), new(S, Depth), new(S, DepthExtend), new(E, DepthExtend)},
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

public static class ImageUtils
{
    public static void FixFutileTexture(this IImageProcessingContext ctx, int width, int height)
    {
        Size size = ctx.GetCurrentSize();

        if (size.Width < width || size.Height < height + 1)
        {
            ctx.Resize(
                new Size(
                    int.Max(size.Width, width),
                    int.Max(size.Height, height + 1)
                )
            );
        }
    }

    public static void ReadyFutileTexture(this IImageProcessingContext ctx)
    {
        ctx.ProcessPixelRowsAsVector4(pixels =>
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                Vector4 pixel = pixels[i];
                if (pixel == Vector4.UnitW || pixel == Vector4.One)
                {
                    pixels[i] = Vector4.Zero;
                }
            }
        });
    }

    // Was supposed to fix shadows for extreme sun X angles
    // TODO: FIX SHADOWS FOR THE FINAL TIME
    public static Mesh ExtrudeEdgesAsMesh(this Image<Rgba32> image, Rectangle area, float depth)
    {
        var edges = new List<ContinuousEdge>();
        var rightSides = new List<LREdgePoint>();
        var leftSides = new List<LREdgePoint>();

        image.ProcessPixelRows(_ => {
	        Parallel.For(area.Top, area.Bottom, y => {
		        var mem = image.DangerousGetPixelRowMemory(y);
		        var row = mem.Span;

		        var lastRowAvail = y > area.Top;
		        var lastMem = lastRowAvail ? image.DangerousGetPixelRowMemory(y - 1) : default;
		        var lastRow = lastMem.Span;

		        var nextRowAvail = y < area.Bottom - 2;
		        var nextMem = nextRowAvail ? image.DangerousGetPixelRowMemory(y + 1) : default;
		        var nextRow = nextMem.Span;

                ContinuousEdge? TopEdge = null;
                ContinuousEdge? BottomEdge = null;

		        for (int x = area.Left; x < area.Right; x++)
		        {
			        if (row[x].A == 0) continue;

			        Point px = new(x - area.Left, y - area.Top);

                    lock (edges)
                    {
                        // top
                        if (!lastRowAvail || lastRow[x].A == 0)
                        {
                            TopEdge ??= new ContinuousEdge(px, EdgeExtruderSide.Top, depth);

                            TopEdge.End.X++;

                            if (x >= area.Right - 2 || row[x + 1].A == 0 || !lastRowAvail || lastRow[x + 1].A != 0)
                            {
                                edges.Add(TopEdge);
                                TopEdge = null;
                            }
                        }

                        // right
                        if (x >= area.Right - 2 || row[x + 1].A == 0)
                        {
                            bool endPx = !nextRowAvail || nextRow[x].A == 0 || x >= area.Right - 2 || nextRow[x + 1].A != 0;
                            rightSides.Add(new LREdgePoint(px, endPx));
                        }

                        // bottom
                        if (!nextRowAvail || nextRow[x].A == 0)
                        {
                            BottomEdge ??= new ContinuousEdge(px, EdgeExtruderSide.Bottom, depth);

                            BottomEdge.End.X++;

                            if (x >= area.Right - 2 || row[x + 1].A == 0 || !nextRowAvail || nextRow[x + 1].A != 0)
                            {
                                edges.Add(BottomEdge);
                                BottomEdge = null;
                            }
                        }

                        // left
                        if (x == area.Left || row[x - 1].A == 0)
                        {
                            bool endPx = !nextRowAvail || nextRow[x].A == 0 || x == area.Left || nextRow[x - 1].A != 0;
                            leftSides.Add(new LREdgePoint(px, endPx));
                        }
                    }
		        }
	        });
        });

        foreach (LREdgePoint pt in rightSides)
        {
            if (pt.Consumed) continue;
            pt.Consumed = true;

            ContinuousEdge edge = new(pt.Px, EdgeExtruderSide.Right, depth);

            if (pt.IsEnd)
            {
                edges.Add(edge);
                continue;
            }

            var sameAxisEdges = rightSides.Where(e => e.Px.X == pt.Px.X).OrderBy(e => e.Px.Y);

            foreach (LREdgePoint axisEdge in sameAxisEdges)
            {
                if (axisEdge.Consumed) continue;
                axisEdge.Consumed = true;

                edge.End.Y++;

                if (!axisEdge.IsEnd) continue;
                edges.Add(edge);

                break;
            }
        }

        foreach (LREdgePoint pt in leftSides)
        {
            if (pt.Consumed) continue;
            pt.Consumed = true;

            ContinuousEdge? edge = new(pt.Px, EdgeExtruderSide.Left, depth);

            if (pt.IsEnd)
            {
                edges.Add(edge);
                continue;
            }

            var sameAxisEdges = leftSides.Where(e => e.Px.X == pt.Px.X).OrderBy(e => e.Px.Y);

            foreach (LREdgePoint axisEdge in sameAxisEdges)
            {
                if (axisEdge.Consumed) continue;
                axisEdge.Consumed = true;

                edge.End.Y++;

                if (!axisEdge.IsEnd) continue;
                edges.Add(edge);

                break;
            }
        }

        Mesh mesh = new();
        mesh.ReadyMerge(edges.Count * 4, edges.Count * 6, true);
        foreach (ContinuousEdge line in edges)
        {
	        mesh.MergeQuad(line.ToVertices());
        }

        return mesh;
    }

    public static RgbaFloat ToRgbaFloat(this Rgba32 v)
    {
        return new RgbaFloat(v.ToVector4());
    }

    public static void ReplaceColor(this Image<Rgba32> image, Rgba32 colorGet, Rgba32 colorSet)
    {
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                Rgba32 cPix = image[x, y];

                if (cPix == colorGet)
                {
                    image[x, y] = colorSet;
                }
            }
        }
    }

    public static void CopyArea(this Image<Rgba32> source, Image<Rgba32> destination, Rectangle srcRect, Point dstPos)
    {
        for (int x = 0; x < srcRect.X; x++)
        {
            for (int y = 0; y < srcRect.Y; y++)
            {
                destination[dstPos.X + x, dstPos.Y + y] = source[srcRect.X + x, srcRect.Y + y];
            }
        }
    }

    public static void CopyAreaMix(this Image<Rgba32> source, Image<Rgba32> destination, Point srcPos, Point dstPos, Point size, float amount)
    {
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                if (source[srcPos.X + x, srcPos.Y + y] == new Rgba32(0)) continue;

                destination[dstPos.X + x, dstPos.Y + y] = destination[dstPos.X + x, dstPos.Y + y].MixRGB(source[srcPos.X + x, srcPos.Y + y], amount);
            }
        }
    }

    public static Image<Rgba32> ImageMix(Image<Rgba32> top, Image<Rgba32> bottom, Vector2Int size, float amount)
    {
        Image<Rgba32> outImg = new Image<Rgba32>(size.X, size.Y);

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                outImg[x, y] = MixRGB(top[x, y], bottom[x, y], amount);
            }
        }

        return outImg;
    }

    public static RgbaFloat MixRgbaFloat(this RgbaFloat top, RgbaFloat bottom, float amount)
    {
        Vector4 topC = top.ToVector4();
        Vector4 bottomC = bottom.ToVector4();
        Vector4 mixed = Vector4.One * (topC * amount + bottomC * (1 - amount));
        mixed.W = 1;

        return new RgbaFloat(mixed);
    }

    public static Rgba32 MixRGB(this Rgba32 top, Rgba32 bottom, float amount)
    {
        Vector4 topC = top.ToVector4();
        Vector4 bottomC = bottom.ToVector4();
        Vector4 mixed = Vector4.One * (topC * amount + bottomC * (1 - amount));
        mixed.W = 1;

        return new Rgba32(mixed);
    }

    public static Image<Rgba32> ToImage(GraphicsDevice device, Texture texture)
    {
        if (texture.Format != PixelFormat.R8_G8_B8_A8_UNorm) throw new ArgumentException("Texture must use R8_G8_B8_A8_UNorm format!");
        if ((texture.Usage & TextureUsage.Staging) == 0) throw new ArgumentException("Only staging textures may be converted to images!");

        var map = device.Map(texture, MapMode.Read);

        unsafe
        {
            byte* src = (byte*)map.Data;
            byte[] dst = new byte[texture.Width * texture.Height * sizeof(Rgba32)];
            byte* end = src + map.SizeInBytes;

            int y = 0;
            while (src < end)
            {
                Marshal.Copy((IntPtr)src, dst, y * (int)texture.Width * 4, (int)texture.Width * 4);
                src += map.RowPitch;
                y++;
            }

            device.Unmap(texture);

            return Image.LoadPixelData<Rgba32>(dst, (int)texture.Width, (int)texture.Height);
        }
    }
}