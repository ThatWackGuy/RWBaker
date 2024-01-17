using System.Diagnostics;
using System.IO;
using ImGuiNET;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;

namespace RWBaker.TileTools;

public class RenderAllTiles : Window
{
    private RWScene scene;
    private Vector2Int renderOffset;
    private Stopwatch renderTime;
    
    public RenderAllTiles() : base("Render Tiles", "render_tiles")
    {
        scene = new RWScene();
        renderOffset = Vector2Int.Zero;
        renderTime = new Stopwatch();
    }

    protected override void Draw()
    {
        Begin();
        ImGui.TextDisabled($"Using Graphics Dir '{context.SavedGraphicsDir}'");
        ImGui.TextDisabled($"Using Palette: {PaletteManager.CurrentPalette.Name}");
        ImGui.TextDisabled("Bulk renders always use the first variation of a tile!");
        ImGui.Separator();
        
        ImGui.SliderInt("Layer Offset X", ref renderOffset.X, -50, 50);
        ImGui.SliderInt("Layer Offset Y", ref renderOffset.Y, -50, 50);
        
        ImGui.Checkbox("Use Unlit Palette", ref context.TileUseUnlit);

        if (ImGui.Button("RENDER TILES"))
        {
            Directory.CreateDirectory(context.SavedGraphicsDir + "/Rendered/Bulk/");

            renderTime.Restart();
            foreach (Tile tile in Program.Tiles)
            {
                tile.CacheTexture(context);
                scene.Resize(tile);
                scene.AddObject(tile);
                scene.Render(context.TileUseUnlit);
                tile.DisposeTexture();
                
                scene.SaveToFile($"{context.SavedGraphicsDir}/Rendered/Bulk/{tile.Name}.png");
            }
            renderTime.Stop();
        }
        
        ImGui.TextDisabled($"Render Time: {renderTime.ElapsedMilliseconds} ms.");
        
        ImGui.End();
    }

    protected override void Destroy()
    {
        scene.Dispose();
    }
}