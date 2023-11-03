using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;

namespace RWBaker.TileTools;

public class TileLoader : Window
{
    private readonly Context context;
    
    private string getTilesLog;
    
    public TileLoader() : base("RW Graphics Parser", "rw-baker-parser")
    {
        Open = true;

        context = Context.GetContext();

        getTilesLog = "";
    }
    
    protected override void Draw()
    {
        Begin();
        
        ImGui.InputTextWithHint("Graphics Path", "Graphics Folder Location", ref context.SavedGraphicsDir, 280);

        if (ImGui.Button("Get Tiles"))
        {
            Program.Tiles.Clear();
            RWUtils.GetTiles(out getTilesLog);
        }
        
        foreach (string ln in getTilesLog.Split("\n"))
        {
            if (ln == "") continue;
            ImGui.TextDisabled(ln);
        }
    }
}