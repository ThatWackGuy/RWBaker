using System;
using RWBaker.Gui;
using Veldrid;

namespace RWBaker.Rendering;

public class RenderPass : IDisposable
{
    public readonly string Id;

    public readonly GuiTexture RenderTexture;
    public readonly Texture DepthTexture;

    public readonly Framebuffer Framebuffer;
    public readonly OutputDescription OutputDescription;

    public readonly FaceCullMode FaceCullMode;
    public readonly BlendStateDescription BlendStateDescription;

    public RenderPass(ResourceFactory factory, string id, BlendStateDescription blendState, uint width = 1, uint height = 1, FaceCullMode faceCullMode = FaceCullMode.Back)
    {
        Id = id;

        Texture renderTarget = factory.CreateTexture(
            new TextureDescription(
                width,
                height,
                1,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget | TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        RenderTexture = GuiTexture.Create($"_pass_color_{id}", renderTarget);

        DepthTexture = factory.CreateTexture(
            new TextureDescription(
                width,
                height,
                1,
                1,
                1,
                PixelFormat.D24_UNorm_S8_UInt,
                TextureUsage.DepthStencil | TextureUsage.Sampled,
                TextureType.Texture2D
            )
        );
        DepthTexture.Name = $"_pass_depth_{id}";

        Framebuffer = factory.CreateFramebuffer(new FramebufferDescription(
                DepthTexture, renderTarget
            )
        );
        Framebuffer.Name = $"_pass_framebuffer_{id}";

        OutputDescription = Framebuffer.OutputDescription;

        FaceCullMode = faceCullMode;
        BlendStateDescription = blendState;
    }

    public void Dispose()
    {
        RenderTexture.Dispose();
        DepthTexture.Dispose();

        Framebuffer.Dispose();
    }
}