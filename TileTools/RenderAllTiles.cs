using System.IO;
using ImGuiNET;
using RWBaker.GraphicsTools;

namespace RWBaker.TileTools;

public class RenderAllTiles : Window
{
    private RWScene scene;
    private Vector2Int renderOffset;
    private long renderTime;
    
    public RenderAllTiles() : base("Render Tiles", "render_tiles")
    {
        scene = new RWScene();
        renderOffset = Vector2Int.Zero;
        renderTime = 0;
    }

    protected override void Draw()
    {
        Begin();
        ImGui.TextDisabled($"Using Graphics Dir '{context.SavedGraphicsDir}'");
        ImGui.TextDisabled($"Using Palette: {Program.CurrentPalette.Name}");
        ImGui.TextDisabled("Bulk renders always use the first variation of a tile!");
        ImGui.Separator();
        
        ImGui.SliderInt("Layer Offset X", ref renderOffset.X, -50, 50);
        ImGui.SliderInt("Layer Offset Y", ref renderOffset.Y, -50, 50);
        
        ImGui.Checkbox("Use Unlit Palette", ref context.TileUseUnlit);

        if (ImGui.Button("RENDER TILES"))
        {
            bool saved = context.TileOutputToFile;
            context.TileOutputToFile = true;
            
            Directory.CreateDirectory(context.SavedGraphicsDir + "/RENDERED/");

            foreach (Tile tile in Program.Tiles)
            {
                scene.Resize(tile);
                scene.AddObject(tile);
                scene.Render(context.TileUseUnlit);
                
                if (context.TileOutputToFile) scene.SaveToFile($"{context.SavedGraphicsDir}/RENDERED/{tile.Name}.png");
            }

            context.TileOutputToFile = saved;
        }
        
        ImGui.TextDisabled($"Render Time: {renderTime} ms.");
        
        ImGui.End();
    }

    protected override void Destroy()
    {
        scene.Dispose();
    }
}