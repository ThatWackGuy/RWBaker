using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace RWBaker;

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

    public static void ReadyFutileTexture(this IImageProcessingContext ctx, bool skipBlack)
    {
        ctx.ProcessPixelRowsAsVector4(pixels =>
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                Vector4 pixel = pixels[i];
                if (pixel == Vector4.One || (pixel == Vector4.UnitW && !skipBlack))
                {
                    pixels[i] = Vector4.Zero;
                }
            }
        });
    }

    public static RgbaFloat ToRgbaFloat(this Rgba32 v)
    {
        return new RgbaFloat(v.ToVector4());
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