using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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

    /// <summary>
    /// Fetches all registered textures as a <see cref="ReadOnlySequence{T}"/>
    /// </summary>
    /// <remarks>
    /// Not recommended for general use. Please use <see cref="TryGetTexture(System.IntPtr,out RWBaker.Gui.GuiTexture)"/> or similar.
    /// </remarks>
    public static ImmutableArray<GuiTexture> GetAllTextures()
    {
        return _mapByIdx.Values.ToImmutableArray();
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

    public Texture ToStaging(CommandList commandList)
    {
        GraphicsDevice graphics = GuiManager.GraphicsDevice;
        ResourceFactory factory = GuiManager.ResourceFactory;

        // Copy the color render target into a staging texture
        Texture stagingTex = factory.CreateTexture(
            new TextureDescription(
                Texture.Width,
                Texture.Height,
                Texture.Depth,
                Texture.MipLevels,
                Texture.ArrayLayers,
                Texture.Format,
                TextureUsage.Staging,
                Texture.Type
            )
        );
        using Fence fence = factory.CreateFence(false);

        commandList.Begin();
        commandList.CopyTexture(Texture, stagingTex);
        commandList.End();
        graphics.SubmitCommands(commandList, fence);
        graphics.WaitForFence(fence);

        return stagingTex;
    }

    public Image<Rgba32> ToImage(bool force = false)
    {
        bool usingTempStaging = false;
        Texture texture = Texture;

        if (texture.Format != PixelFormat.R8_G8_B8_A8_UNorm) throw new ArgumentException("Texture must use R8_G8_B8_A8_UNorm format!");
        if ((texture.Usage & TextureUsage.Staging) == 0)
        {
            if (!force) throw new ArgumentException("Only staging textures may be converted to images!");

            texture = ToStaging(GuiManager.CommandList);
            usingTempStaging = true;
        }

        GraphicsDevice graphics = GuiManager.GraphicsDevice;

        MappedResource map = graphics.Map(texture, MapMode.Read);

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

            graphics.Unmap(texture);

            Image<Rgba32> ret = Image.LoadPixelData<Rgba32>(dst, (int)texture.Width, (int)texture.Height);
            if (usingTempStaging) texture.Dispose();

            return ret;
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