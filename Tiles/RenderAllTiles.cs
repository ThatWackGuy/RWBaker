using System.Diagnostics;
using System.IO;
using ImGuiNET;
using RWBaker.Palettes;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using RWBaker.Windows;

namespace RWBaker.Tiles;

public class RenderAllTiles : Window
{
    private RWObjectManager objects => Program.ObjectManager;
    private PaletteManager palettes => Program.PaletteManager;

    private readonly Scene scene;

    private Vector2Int renderOffset;
    private Stopwatch renderTime;

    public RenderAllTiles() : base("Render Tiles", "render_tiles")
    {
        scene = new Scene();

        renderOffset = Vector2Int.Zero;
        renderTime = new Stopwatch();
    }

    protected override void Draw()
    {
        Begin();

        if (objects.Tiles.Count == 0)
        {
            ImGui.Text("NO TILES FOUND!");
            ImGui.End();
            return;
        }

        ImGui.TextDisabled(objects.GraphicsDir);
        ImGui.TextDisabled(palettes.CurrentPalette.Name);
        ImGui.TextDisabled("Bulk renders always use the first variation of a tile!");
        ImGui.Separator();

        ImGui.SliderInt("Layer Offset X", ref renderOffset.X, -50, 50);
        ImGui.SliderInt("Layer Offset Y", ref renderOffset.Y, -50, 50);

        ImGui.Checkbox("Use Unlit Palette", ref objects.TileUseUnlit);
        ImGui.Checkbox("Use Rain Palette", ref objects.TileUseRain);

        if (ImGui.Button("RENDER TILES"))
        {
            Directory.CreateDirectory(objects.GraphicsDir + "/Rendered/Bulk/");

            renderTime.Restart();
            foreach (TileInfo tile in objects.Tiles)
            {
                if (!File.Exists(Path.Combine(objects.GraphicsDir, $"{tile.Name}.png"))) continue;
                using TileObject cache = new(scene, objects, tile);
                scene.Resize(cache);
                scene.AddObject(cache);
                scene.Unlit = objects.TileUseUnlit;
                scene.Rain = objects.TileUseRain;
                scene.Render();
                scene.RemoveObject(cache);

                scene.SaveToFile($"{objects.GraphicsDir}/Rendered/Bulk/{tile.Name}.png");
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