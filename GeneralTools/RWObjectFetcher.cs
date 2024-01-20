using System.Numerics;
using ImGuiNET;

namespace RWBaker.GeneralTools;

public class RWObjectFetcher : Window
{
    private string getTilesLog;
    private string getPropsLog;
    private Vector2 childDefSize;
    
    public RWObjectFetcher() : base("Get Graphics", "_GraphicsFetcher")
    {
        getTilesLog = "";
        getPropsLog = "";
        childDefSize = new Vector2(0, 80);
    }

    protected override void Draw()
    {
        Begin();

        ImGui.SeparatorText("Tiles");
        
        if (ImGui.BeginChild("TileFetcher", childDefSize, ImGuiChildFlags.Border))
        {
            ImGui.InputTextWithHint("Graphics Path", "Graphics Folder Location", ref context.SavedGraphicsDir, 540);

            if (ImGui.Button("Get Tiles"))
            {
                Program.Tiles.Clear();
                RWUtils.GetTiles(context, out getTilesLog);
            }

            if (getTilesLog.Length > 0) Utils.WarningMarker(getTilesLog);
            
            ImGui.EndChild();
        }
        
        ImGui.SeparatorText("Props");

        if(ImGui.BeginChild("PropFetcher", childDefSize, ImGuiChildFlags.Border))
        {
            ImGui.InputTextWithHint("Props Path", "Props Folder Location", ref context.SavedPropsDir, 540);

            if (ImGui.Button("Get Props"))
            {
                Program.Props.Clear();
                RWUtils.GetProps(context, out getPropsLog);
            }
        
            if (getPropsLog.Length > 0) Utils.WarningMarker(getPropsLog);
            
            ImGui.EndChild();
        }
        
        ImGui.SeparatorText("Palettes");
        
        if(ImGui.BeginChild("PaletteFetcher", childDefSize, ImGuiChildFlags.Border))
        {
            ImGui.InputTextWithHint("Palettes Path", "Palette Folder Location", ref context.SavedPaletteDir, 280);

            if (ImGui.Button("Get palettes"))
            {
                PaletteManager.Unload();
                PaletteManager.Load(context);
            }
            
            ImGui.EndChild();
        }
        
        ImGui.End();
    }

    protected override void Destroy()
    {
        
    }
}