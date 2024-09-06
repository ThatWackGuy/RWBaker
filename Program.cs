using System;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using RWBaker.Gui;
using RWBaker.Nodes;
using RWBaker.Palettes;
using SixLabors.ImageSharp;
using Vortice.ShaderCompiler;

namespace RWBaker;

public static class Program
{
    public static RWObjectManager ObjectManager { get; private set; } = null!;
    public static PaletteManager PaletteManager { get; private set; } = null!;
    public static SceneBuilder SceneBuilder { get; private set; } = null!;

    public static void Main()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;

        UserData userData = UserData.Load("./userdata.json");

        userData.Save("./userdata.json");

        GuiManager.Load(userData);
        if (userData.FailedLoad != null) GuiManager.Exception(userData.FailedLoad);

        PaletteManager = new PaletteManager(userData);
        ThreadPool.QueueUserWorkItem(_ => PaletteManager.GetPalettes(userData.SavedPaletteDir));

        ObjectManager = new RWObjectManager(userData);
        ThreadPool.QueueUserWorkItem(_ => ObjectManager.GetTiles(userData.SavedGraphicsDir));
        ThreadPool.QueueUserWorkItem(_ => ObjectManager.GetProps(userData.SavedPropsDir));

        RWUtils.LoadGraphicsResources();
        PreValidateShaders();

        NodeBoard.RegisterNode<ConstantInt>();
        NodeBoard.RegisterNode<ConstantAdder>();

        SceneBuilder = new SceneBuilder();

        var testg = new NodeBoard("Test Graph");
        GuiManager.AddWindow(testg);

        GuiManager.RenderProcess();
    }

    public static void PreValidateShaders()
    {
        string errors = "";

        foreach (string f in Directory.EnumerateFiles("./shaders"))
        {
            string ext = Path.GetExtension(f).ToLower();
            if (ext is not ".vert" and not ".frag") continue;

            ShaderKind kind = ext switch
            {
                ".vert" => ShaderKind.VertexShader,
                ".frag" => ShaderKind.FragmentShader,

                _ => throw new ArgumentOutOfRangeException()
            };

            try
            {
                Utils.ShaderC(f, kind);
            }
            catch (Exception e)
            {
                errors += $"{e.Message}\n\n";
            }
        }

        if (errors.Length != 0)
        {
            GuiManager.Exception(new Exception(errors), "SHADER PRE-VALIDATIONS FAILED", _ => PreValidateShaders());
        }
    }
}