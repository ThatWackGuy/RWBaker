using System;
using RWBaker.Gui;
using Veldrid;
using Veldrid.SPIRV;

namespace RWBaker.RWObjects;

public static class RWUtils
{
    public static VertexLayoutDescription[] RWVertexLayout;

    public static ResourceLayout RWObjectDataLayout;
    public static ResourceLayout RWObjectTextureLayout;

    public static ShaderSetDescription TileRendererShaderSet;

    public static ShaderSetDescription StandardPropRendererShaderSet;
    public static ShaderSetDescription SoftPropRendererShaderSet;
    public static ShaderSetDescription DecalPropRendererShaderSet;
    public static ShaderSetDescription AntimatterPropRendererShaderSet;

    public static void LoadGraphicsResources()
    {
        ResourceFactory factory = GuiManager.ResourceFactory;

        RWVertexLayout = new VertexLayoutDescription[]
        {
            new(
                new VertexElementDescription(
                    "v_position",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3
                ), // 12 bytes

                new VertexElementDescription(
                    "v_texCoord",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2
                ), // 8 bytes

                new VertexElementDescription(
                    "v_color",
                    VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float4
                ) // 16 bytes
            )
        };
        // 36 bytes for each input

        RWObjectDataLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SceneData", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                new ResourceLayoutElementDescription("RenderData", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)
            )
        );

        RWObjectTextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("PaletteTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("EffectTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
            )
        );

        // TILES
        ShaderDescription tileVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.tile.vert"), "main");
        ShaderDescription tileFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.tile.frag"), "main");
        Shader[] tileShaders = factory.CreateFromSpirv(tileVert, tileFrag);

        TileRendererShaderSet = new ShaderSetDescription(RWVertexLayout, tileShaders);

        // PROPS
        ShaderDescription standardVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.standardprop.vert"), "main");
        ShaderDescription standardFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.standardprop.frag"), "main");
        Shader[] standardPropShaders = factory.CreateFromSpirv(standardVert, standardFrag);

        StandardPropRendererShaderSet = new ShaderSetDescription(RWVertexLayout, standardPropShaders);

        ShaderDescription softVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.softprop.vert"), "main");
        ShaderDescription softFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.softprop.frag"), "main");
        Shader[] softPropShaders = factory.CreateFromSpirv(softVert, softFrag);

        SoftPropRendererShaderSet = new ShaderSetDescription(RWVertexLayout, softPropShaders);

        ShaderDescription decalVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.decalprop.vert"), "main");
        ShaderDescription decalFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.decalprop.frag"), "main");
        Shader[] decalPropShaders = factory.CreateFromSpirv(decalVert, decalFrag);

        DecalPropRendererShaderSet = new ShaderSetDescription(RWVertexLayout, decalPropShaders);

        ShaderDescription antimatterVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.antimatterprop.vert"), "main");
        ShaderDescription antimatterFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.antimatterprop.frag"), "main");
        Shader[] antimatterPropShaders = factory.CreateFromSpirv(antimatterVert, antimatterFrag);

        AntimatterPropRendererShaderSet = new ShaderSetDescription(RWVertexLayout, antimatterPropShaders);
    }

    public static bool LingoBool(string line, out bool value)
    {
        if (int.TryParse(line, out int i))
        {
            value = i != 0;
            return true;

        }

        value = default;
        return false;
    }

    public static bool LingoInt(string line, out int value)
    {
        return int.TryParse(line, out value);
    }

    public static bool LingoFloat(string line, out float value)
    {
        try
        {
            value = Convert.ToSingle(line.Replace('.', ','));
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }

    public static bool LingoEnum<T>(string line, T defaultVal, out T value) where T : struct, Enum
    {
        if (Enum.TryParse(line, true, out value)) return true;

        value = defaultVal;
        return false;
    }

    public static bool LingoIntArray(string line, out int[] array)
    {
        string[] values = line.Replace(" ", "").Split(',');

        if (values[0] != "")
        {
            int layerL = values.Length;

            array = new int[layerL];

            for (int i = 0; i < layerL; i++)
            {
                array[i] = int.Parse(values[i]);
            }

            return true;
        }

        array = new[] { 0 };
        return false;
    }

    public static bool LingoIntArray(string[] values, out int[] array)
    {
        if (values[0] != "")
        {
            int layerL = values.Length;

            array = new int[layerL];

            for (int i = 0; i < layerL; i++)
            {
                array[i] = int.Parse(values[i]);
            }

            return true;
        }

        array = new[] { 0 };
        return false;
    }
}