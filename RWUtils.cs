using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;
using RWBaker.PropTools;
using RWBaker.TileTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.SPIRV;

namespace RWBaker;

public static class RWUtils
{
    public static ResourceLayout RWObjectTextureLayout;

    public static ShaderSetDescription RWShadowShaderSet;

    public static ShaderSetDescription TileRendererShaderSet;
    public static ResourceLayout RWObjectDataLayout;

    public static VertexLayoutDescription[] RWVertexLayout;

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
        
        RWObjectTextureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("PaletteTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ShadowTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
            )
        );
        
        RWObjectDataLayout = factory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("RenderData", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)
            )
        );

        // SHADOWS
        ShaderDescription shadowVert = new(ShaderStages.Vertex, Utils.GetEmbeddedBytes("res.shaders.shadow.vert"), "main");
        ShaderDescription shadowFrag = new(ShaderStages.Fragment, Utils.GetEmbeddedBytes("res.shaders.shadow.frag"), "main");
        Shader[] shadowShaders = factory.CreateFromSpirv(shadowVert, shadowFrag);

        RWShadowShaderSet = new ShaderSetDescription(RWVertexLayout, shadowShaders);
        
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
    
    public static void GetPalettes(Context context)
    {
        if (context.SavedPaletteDir == "") return;
    
        foreach (Palette palette in Program.Palettes)
        {
            palette.DisplayTex.Dispose();
        }
        
        Program.Palettes.Clear();

        foreach (string file in Directory.EnumerateFiles(context.SavedPaletteDir))
        {
            if (Path.GetExtension(file) != ".png") continue;
            
            string fileName = Path.GetFileNameWithoutExtension(file);
            
            if (!fileName.StartsWith("palette")) continue;

            Image<Rgba32> image = Image.Load<Rgba32>(file);
            Palette palette = new(image, fileName);
            
            Program.Palettes.Add(palette);
        }
    }
    
    public static void PaletteCheck(Context context)
    {
        try
        {
            List<Palette> Palettes = Program.Palettes;
            Palette default0 = default;
            Palette A;
            Palette B;
            
            if (context.UsingPalette1 == "" || context.UsingPalette2 == "")
            {
                default0 = new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "DEFAULT_0");
            }
            
            if (context.UsingPalette1 == "" && context.UsingPalette2 == "")
            {
                Program.PaletteA = default0;
                Program.PaletteB = default0;
                Program.CurrentPalette = default0;
                return;
            }

            if (context.UsingPalette1 == "")
            {
                A = default0;
            }
            else
            {
                A = Program.Palettes.Any(p => p.Name == context.UsingPalette1) 
                    ? Palettes.First(p => p.Name == context.UsingPalette1)
                    : new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "UNKNOWN_0");
            }

            if (context.UsingPalette2 == "")
            {
                B = default0;
            }
            else
            {
                B = Palettes.Any(p => p.Name == context.UsingPalette2)
                    ? Palettes.First(p => p.Name == context.UsingPalette2)
                    : new Palette(Image.Load<Rgba32>(Utils.GetEmbeddedBytes("res.palette0.png")), "UNKNOWN_0");
            }

            Program.PaletteA = A;
            Program.PaletteB = B;

            if (context.Palette1Percentage == 100)
            {
                Program.CurrentPalette = A;
                return;
            }
            
            if (context.Palette2Percentage == 100)
            {
                Program.CurrentPalette = B;
                return;
            }
            
            Program.CurrentPalette = Palette.MixPalettes(A, B, context.Palette1Percentage, context.Palette2Percentage);
        }
        catch (Exception e)
        {
            Console.WriteLine($"ERROR WHILE LOADING PALETTES ON STARTUP:\n{e}");
        }
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
            if (Regex.IsMatch(line, "^\\s*$") || Regex.IsMatch(line, "^--\\w*\\n")) continue;
            
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

            try
            {
                // tile definition
                Tile tile = new(line, lastCategory, lastColor, ref log);
                Program.Tiles.Add(tile);
            }
            catch (Exception e)
            {
                log += $"A problem occurred while parsing init line {line} :: {e}\n";
            }
        }

        if (log == "") log += "No errors have occured!\nYou're free to do other stuff!";
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
            
            // tile definition
            Prop prop = new(line, lastCategory, lastColor, ref log);
            Program.Props.Add(prop);
        }

        if (log == "") log += "No errors have occured!\nYou're free to do other stuff!";
    }
}