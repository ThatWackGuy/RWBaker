using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.GraphicsTools;

public class GuiTexture : IDisposable
{
    private static readonly Dictionary<IntPtr, GuiTexture> _map = new();
    private static IntPtr _nextIndex = 1;

    public readonly string Name;
    public readonly IntPtr Index;
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
        GuiTexture tex = new(name, texture, textureLayout ?? GuiManager.TextureLayout, _nextIndex);
        _map.Add(_nextIndex, tex);
        _nextIndex++;
        return tex;
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
        Image<Rgba32> image = Image.Load<Rgba32>(path);

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

    public static GuiTexture GetTexture(IntPtr index)
    {
        return _map[index];
    }

    public static void DisposeAllTextures()
    {
        foreach (var texture in _map)
        {
            texture.Value.Dispose();
        }
        
        _map.Clear();
    }

    public void Dispose()
    {
        _resourceSet?.Dispose();
        _view?.Dispose();
        Texture.Dispose();
        
        _map.Remove(Index);
        
        GC.SuppressFinalize(this);
    }
}