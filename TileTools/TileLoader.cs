using ImGuiNET;

namespace RWBaker.TileTools;

public class TileLoader : Window
{
    private string getTilesLog;
    
    public TileLoader() : base("RW Graphics Parser", "rw-baker-parser")
    {
        getTilesLog = "";
    }
    
    protected override void Draw()
    {
        Begin();
        
        ImGui.InputTextWithHint("Graphics Path", "Graphics Folder Location", ref context.SavedGraphicsDir, 280);

        if (ImGui.Button("Get Tiles"))
        {
            Program.Tiles.Clear();
            RWUtils.GetTiles(context, out getTilesLog);
        }
        
        foreach (string ln in getTilesLog.Split("\n"))
        {
            if (ln == "") continue;
            ImGui.TextDisabled(ln);
        }
    }

    protected override void Destroy()
    {
        
    }
}