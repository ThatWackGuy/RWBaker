using System;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker;

// Technically not required anymore but these were a pain in the ass to code
public static class ImageUtils
{
    public static Vector2Int IntSize(this Image<Rgba32> image)
    {
        return new Vector2Int(image.Width, image.Height);
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

    public static void CopyArea(this Image<Rgba32> source, Image<Rgba32> destination, Vector2Int srcPos, Vector2Int dstPos, Vector2Int size)
    {
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                destination[dstPos.X + x, dstPos.Y + y] = source[srcPos.X + x, srcPos.Y + y];
            }
        }
    }
    
    public static void CopyAreaMix(this Image<Rgba32> source, Image<Rgba32> destination, Vector2Int srcPos, Vector2Int dstPos, Vector2Int size, float topPercentage, float bottomPercentage)
    {
        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                if (source[srcPos.X + x, srcPos.Y + y] == new Rgba32(0)) continue;
                
                destination[dstPos.X + x, dstPos.Y + y] = destination[dstPos.X + x, dstPos.Y + y].MixRGB(source[srcPos.X + x, srcPos.Y + y], topPercentage, bottomPercentage);
            }
        }
    }
    
    public static Image<Rgba32> ImageMix(Image<Rgba32> top, Image<Rgba32> bottom, Vector2Int size, float topPercentage, float bottomPercentage)
    {
        Image<Rgba32> outImg = new Image<Rgba32>(size.X, size.Y);

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                outImg[x, y] = MixRGB(top[x, y], bottom[x, y], topPercentage, bottomPercentage);
            }
        }

        return outImg;
    }

    public static Rgba32 MixRGB(this Rgba32 top, Rgba32 bottom, float topA, float bottomA)
    {
        Vector4 topC = top.ToVector4();
        Vector4 bottomC = bottom.ToVector4();
        Vector4 mixed = topC * topA + bottomC * bottomA * (1 - topA);

        return new Rgba32(mixed);
    }
    
    public static void Expand(ref Image<Rgba32> originImage, int newWidth, int newHeight)
    {
        Image<Rgba32> newImage = new(int.Max(originImage.Width, newWidth), int.Max(originImage.Height, newHeight));
        originImage.CopyArea(newImage, Vector2Int.Zero, Vector2Int.Zero, originImage.IntSize());
        originImage = newImage;
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