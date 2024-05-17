using System;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.Gui;

public class GuiTexture : IDisposable
{
    private static readonly ConcurrentDictionary<IntPtr, GuiTexture> _mapByIdx = new();
    private static readonly ConcurrentDictionary<string, GuiTexture> _mapByName = new();
    private static IntPtr _nextIndex = 1;

    public readonly string Name;
    public readonly IntPtr Index;
    public int RefCounter { get; private set; }
    public readonly Vector2 Size;
    public Texture Texture { get; }

    private readonly TextureView? _view;
    public TextureView View
    {
        get
        {
            if (_view == null)
            {
                throw new NullReferenceException("Non-Sampled Textures Cannot Have a View!");
            }

            return _view;
        }
    }

    private readonly ResourceSet? _resourceSet;
    public ResourceSet ResourceSet
    {
        get
        {
            if (_resourceSet == null)
            {
                throw new NullReferenceException("Non-Sampled Textures Cannot Have a Texture ResourceSet!");
            }

            return _resourceSet;
        }
    }

    private GuiTexture(string name, Texture texture, ResourceLayout textureLayout, IntPtr handle)
    {
        Name = name;
        Index = handle;
        Size = new Vector2(texture.Width, texture.Height);

        Texture = texture;
        Texture.Name = name;

        if ((texture.Usage & TextureUsage.Sampled) != 0)
        {
            TextureView view = GuiManager.ResourceFactory.CreateTextureView(texture);
            _view = view;
            _resourceSet = GuiManager.ResourceFactory.CreateResourceSet(new ResourceSetDescription(textureLayout, _view));
        }
    }

    public static GuiTexture Create(string name, Texture texture, ResourceLayout? textureLayout = null)
    {
        if (_mapByName.ContainsKey(name)) throw new DuplicateNameException($"Texture with name '{name}' already exists");

        GuiTexture tex = new(name, texture, textureLayout ?? GuiManager.TextureLayout, _nextIndex);
        if (!_mapByIdx.TryAdd(_nextIndex, tex)) throw new Exception("Couldn't add texture as Id");
        if(!_mapByName.TryAdd(name, tex)) throw new Exception("Couldn't add texture as Name");
        _nextIndex++;

        return tex;
    }

    public static GuiTexture CreateFromEmbedded(string name, string path, ResourceLayout? textureLayout = null)
    {
        Image<Rgba32> image = Image.Load<Rgba32>(Utils.GetEmbeddedBytes(path));

        Texture texture = GuiManager.ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );

        GuiManager.UpdateTextureFromImage(texture, image);

        return Create(name, texture, textureLayout);
    }

    public static GuiTexture CreateFromImage(string name, Image<Rgba32> image, ResourceLayout? textureLayout = null)
    {
        Texture texture = GuiManager.ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );

        GuiManager.UpdateTextureFromImage(texture, image);

        return Create(name, texture, textureLayout);
    }

    public static GuiTexture CreateFromImage(string name, string path, ResourceLayout? textureLayout = null)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(path);

        Texture texture = GuiManager.ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );

        GuiManager.UpdateTextureFromImage(texture, image);

        return Create(name, texture, textureLayout);
    }

    public static GuiTexture CreateFromImage(string name, ReadOnlySpan<byte> bytes, ResourceLayout? textureLayout = null)
    {
        using Image<Rgba32> image = Image.Load<Rgba32>(bytes);

        Texture texture = GuiManager.ResourceFactory.CreateTexture(
            new TextureDescription(
                (uint)image.Width,
                (uint)image.Height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );

        GuiManager.UpdateTextureFromImage(texture, image);

        return Create(name, texture, textureLayout);
    }

    public static GuiTexture GetTextureSafe(IntPtr index)
    {
        return _mapByIdx.TryGetValue(index, out GuiTexture? texture) ? texture : GuiManager.MissingTex;
    }

    public static bool TryGetTexture(IntPtr index, [MaybeNullWhen(false)] out GuiTexture texture)
    {
        return _mapByIdx.TryGetValue(index, out texture);
    }

    public static bool TryGetTexture(string name, [MaybeNullWhen(false)] out GuiTexture texture)
    {
        return _mapByName.TryGetValue(name, out texture);
    }

    public static bool TextureExists(IntPtr index) => _mapByIdx.ContainsKey(index);
    public static bool TextureExists(string name) => _mapByName.ContainsKey(name);

    public Image<Rgba32> ToImage()
    {
        if (Texture.Format != PixelFormat.R8_G8_B8_A8_UNorm) throw new ArgumentException("Texture must use R8_G8_B8_A8_UNorm format!");
        if ((Texture.Usage & TextureUsage.Staging) == 0) throw new ArgumentException("Only staging textures may be converted to images!");

        GraphicsDevice graphics = GuiManager.GraphicsDevice;

        MappedResource map = graphics.Map(Texture, MapMode.Read);

        unsafe
        {
            byte* src = (byte*)map.Data;
            byte[] dst = new byte[Texture.Width * Texture.Height * sizeof(Rgba32)];
            byte* end = src + map.SizeInBytes;

            int y = 0;
            while (src < end)
            {
                Marshal.Copy((IntPtr)src, dst, y * (int)Texture.Width * 4, (int)Texture.Width * 4);
                src += map.RowPitch;
                y++;
            }

            graphics.Unmap(Texture);

            return Image.LoadPixelData<Rgba32>(dst, (int)Texture.Width, (int)Texture.Height);
        }
    }

    public void Use() => RefCounter++;

    public void Release()
    {
        RefCounter--;

        switch (RefCounter)
        {
            case < 0:
                throw new Exception("Texture Reference Counter shouldn't have -1 references");
            case 0:
                Dispose();
                break;
        }
    }

    public static void DisposeAllTextures()
    {
        foreach (var texture in _mapByIdx)
        {
            texture.Value.Dispose();
        }

        _mapByIdx.Clear();
        _mapByName.Clear();
    }

    public void Dispose()
    {
        _resourceSet?.Dispose();
        _view?.Dispose();
        Texture.Dispose();

        if (!_mapByIdx.TryRemove(Index, out _)) throw new Exception("Couldn't remove texture from id map!");
        if (!_mapByName.TryRemove(Name, out _)) throw new Exception("Couldn't remove texture from name map!");

        GC.SuppressFinalize(this);
    }
}