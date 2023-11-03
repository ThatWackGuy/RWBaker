using System;
using System.Numerics;
using Veldrid;

namespace RWBaker.GraphicsTools;

public struct Tex2D : IDisposable
{
    public readonly string Name;
    public readonly IntPtr Handle;
    public readonly Vector2 Size;
    
    public Texture VTex { get; }
    
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

    public Tex2D(string name, Texture texture, IntPtr handle = 0)
    {
        Name = name;
        Handle = handle;
        Size = new Vector2(texture.Width, texture.Height);
        
        VTex = texture;

        if ((texture.Usage & TextureUsage.Sampled) != 0)
        {
            TextureView view = Graphics.GraphicsDevice.ResourceFactory.CreateTextureView(texture);
            _view = view;
            _resourceSet = Graphics.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(Graphics.TextureLayout, _view));
        }
        else
        {
            _view = null;
            _resourceSet = null;
        }
    }
    
    public Tex2D(string name, Texture texture, ResourceLayout textureLayout, IntPtr handle = 0)
    {
        Name = name;
        Handle = handle;
        Size = new Vector2(texture.Width, texture.Height);

        VTex = texture;
        
        if ((texture.Usage & TextureUsage.Sampled) != 0)
        {
            TextureView view = Graphics.GraphicsDevice.ResourceFactory.CreateTextureView(texture);
            _view = view;
            _resourceSet = Graphics.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(textureLayout, _view));
        }
        else
        {
            _view = null;
            _resourceSet = null;
        }
    }
    
    public void Copy(Tex2D destination, uint x = 0, uint y = 0, uint arrayLayer = 0)
    {
        MappedResource data = Graphics.GraphicsDevice.Map(VTex, MapMode.Read);

        Graphics.GraphicsDevice.UpdateTexture(
            destination.VTex,
            data.Data,
            data.SizeInBytes,
            x,
            y,
            0,
            destination.VTex.Width,
            destination.VTex.Height,
            1,
            0,
            arrayLayer
        );
    }

    public readonly void Dispose()
    {
        _view?.Dispose();
        _resourceSet?.Dispose();
    }
}