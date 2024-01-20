using System;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.GraphicsTools;
using RWBaker.TileTools;
using Veldrid;
using Veldrid.SPIRV;

namespace RWBaker;

public static class RWUtils
{
    public static VertexLayoutDescription[] RWVertexLayout;

    public static ResourceLayout RWObjectDataLayout;
    public static ResourceLayout RWObjectTextureLayout;
    
    public static ShaderSetDescription TileRendererShaderSet;
    
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
        Shader[] TileShaders = factory.CreateFromSpirv(tileVert, tileFrag);

        TileRendererShaderSet = new ShaderSetDescription(RWVertexLayout, TileShaders);
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
            value = Convert.ToSingle(line);
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
    
    public static void GetTiles(Context context, out string log)
    {
        log = "";

        if (!Directory.Exists(context.SavedGraphicsDir))
        {
            log = "GRAPHICS FOLDER DOES NOT EXIST!";
            return;
        }

        if (!File.Exists(context.SavedGraphicsDir + "/init.txt"))
        {
            log = "GRAPHICS INIT DOES NOT EXIST!";
            return;
        }

        string[] initLines = File.ReadAllLines(context.SavedGraphicsDir + "/init.txt");
        
        string lastCategory = "Uncategorized";
        Vector3 lastColor = new(255, 255, 255);

        foreach (string line in initLines)
        {
            // empty line or comment
            if (Regex.IsMatch(line, "^\\s*$") || line.StartsWith("--")) continue;
            
            // category definition
            if (Regex.IsMatch(line, "-\\[\"(.+?)\",\\s*color\\s*\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]"))
            {
                GroupCollection categoryInfo = Regex.Match(line, "-\\[\"(.+?)\",\\s*color\\s*\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]").Groups;
                lastCategory = categoryInfo[1].Value;
                string[] colorsNums = categoryInfo[2].Value.Replace(" ", "").Split(',');
                lastColor.X = int.Parse(colorsNums[0]);
                lastColor.Y = int.Parse(colorsNums[1]);
                lastColor.Z = int.Parse(colorsNums[2]);
                
                continue;
            }

            Tile tile = new(line, lastCategory, lastColor, ref log);
            Program.Tiles.Add(tile);
        }
    }
    
    public static void GetProps(Context context, out string log)
    {
        log = "";

        if (!Directory.Exists(context.SavedPropsDir))
        {
            log = "PROPS FOLDER DOES NOT EXIST!";
            return;
        }

        if (!File.Exists(context.SavedPropsDir + "/init.txt"))
        {
            log = "PROPS INIT DOES NOT EXIST!";
            return;
        }

        string[] initLines = File.ReadAllLines(context.SavedPropsDir + "/init.txt");
        
        string lastCategory = "Uncategorized";
        Vector3 lastColor = new(255, 255, 255);

        foreach (string line in initLines)
        {
            // empty line or comment
            if (Regex.IsMatch(line, "^\\s*$") || Regex.IsMatch(line, "^--\\w*\\n")) continue;
            
            // category definition
            if (Regex.IsMatch(line, "-\\[\"(.+?)\",\\s*color\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]"))
            {
                GroupCollection categoryInfo = Regex.Match(line, "-\\[\"(.+?)\",\\s*color\\(((?:\\s*[0-9]+\\s*,?){3})\\)\\]").Groups;
                lastCategory = categoryInfo[1].Value;
                string[] colorsNums = categoryInfo[2].Value.Replace(" ", "").Split(',');
                lastColor.X = int.Parse(colorsNums[0]);
                lastColor.Y = int.Parse(colorsNums[1]);
                lastColor.Z = int.Parse(colorsNums[2]);
                
                continue;
            }
            
            // Prop prop = new(line, lastCategory, lastColor, ref log);
            // Program.Props.Add(prop);
        }
    }
}