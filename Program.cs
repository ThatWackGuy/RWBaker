using System;
using System.IO;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.RWObjects;
using SixLabors.ImageSharp;

namespace RWBaker;

public static class Program
{
    public static RWObjectManager ObjectManager { get; private set; } = null!;
    public static PaletteManager PaletteManager { get; private set; } = null!;

    public static void Main()
    {
        Configuration.Default.PreferContiguousImageBuffers = true;

        UserData userData;
        Exception? userdataFailed = null;
        // Create the userdata file if it doesn't exist
        if (!File.Exists("./userdata.json"))
        {
            File.Create("./userdata.json").Close();

            userData = new UserData();
        }
        else
        {
            try
            {
                userData = UserData.Load("./userdata.json");
            }
            catch (Exception e)
            {
                userdataFailed = e;
                userData = new UserData();
            }
        }

        userData.Save("./userdata.json");

        GuiManager.Load(userData);

        PaletteManager = new PaletteManager(userData);
        PaletteManager.GetPalettes(userData.SavedPaletteDir);

        ObjectManager = new RWObjectManager(userData);
        ObjectManager.GetTiles(userData.SavedGraphicsDir);
        ObjectManager.GetProps(userData.SavedPropsDir);

        if (userdataFailed != null)
        {
            GuiManager.Exception(userdataFailed);
        }

        RWUtils.LoadGraphicsResources();

        GuiManager.RenderProcess();
    }
}