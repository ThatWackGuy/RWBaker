using System;
using RWBaker.GeneralTools;
using Veldrid;

namespace RWBaker.GraphicsTools;

public struct RWRenderDescription : IDisposable
{
    public readonly RWVertexData[] Vertices;
    public readonly ushort[] Indices;
    public readonly ResourceSet TextureSet;
    public readonly bool HasTextureSet;
    
    public readonly DeviceBuffer SceneDataBuffer;
    public readonly DeviceBuffer ObjectDataBuffer;
    public readonly ResourceSet ObjectData;
    
    public RWSceneInfo SceneInfo;
    public readonly Pipeline Pipeline;

    public RWRenderDescription(RWVertexData[] vertices, ushort[] indices, IRWRenderable renderable, RWScene scene)
    {
        Vertices = vertices;
        Indices = indices;
        
        HasTextureSet = renderable.GetTextureSet(scene, out ResourceSet? textureSet);
        TextureSet = textureSet!;

        SceneInfo = new RWSceneInfo(scene, renderable);
        SceneDataBuffer = GuiManager.ResourceFactory.CreateStructBuffer<RWSceneInfo>();

        ObjectDataBuffer = renderable.CreateObjectData(scene);
        ObjectData = GuiManager.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(
                RWUtils.RWObjectDataLayout,
                SceneDataBuffer,
                ObjectDataBuffer
            )
        );
        
        ResourceLayout[] layouts = { RWUtils.RWObjectDataLayout, RWUtils.RWObjectTextureLayout };
        Pipeline = GuiManager.ResourceFactory.CreateGraphicsPipeline(
            new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(true, true, ComparisonKind.Less),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                renderable.GetShaderSetDescription(),
                layouts,
                scene.ObjectFramebuffer.OutputDescription
            )
        );
    }
    
    public void Dispose()
    {
        Pipeline.Dispose();
        ObjectData.Dispose();
        ObjectDataBuffer.Dispose();
        SceneDataBuffer.Dispose();
        TextureSet.Dispose();
    }
}