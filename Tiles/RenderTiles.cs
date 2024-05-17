using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.Rendering;
using RWBaker.Windows;
using Veldrid;

namespace RWBaker.Tiles;

public class RenderSingleTiles : Window
{
    private RWObjectManager objects => Program.ObjectManager;
    private PaletteManager palettes => Program.PaletteManager;

    private readonly Scene scene;
    private readonly Camera camera;

    private TileObject tile;
    private IEnumerable<TileInfo>? searchedTiles;
    private int variation;
    private CameraBgState background;
    private bool needsRerender;
    private bool sceneSizeChanged;
    private long renderTime;
    private float sizing;
    private int shadowRepeat;

    private readonly Vector4 rectOutlineCol;

    private int renderAllCounter;
    private int renderAllSize;

    public RenderSingleTiles() : base("Render Tiles", "_renderTiles")
    {
        scene = new Scene();
        camera = new Camera(scene);

        scene.AddObject(camera);
        scene.SetActiveCamera(camera);

        if (objects.Tiles.Count == 0)
        {
            tile = new TileObject(); // use empty cache
        }
        else if (objects.TileLastUsed == string.Empty || objects.Tiles.All(t => t.ProperName != objects.TileLastUsed))
        {
            tile = new TileObject(scene, objects, objects.Tiles[0]);
        }
        else
        {
            tile = new TileObject(scene, objects, objects.Tiles.First(t => t.ProperName == objects.TileLastUsed));
        }

        scene.AddObject(tile);

        searchedTiles = null;

        variation = 0;

        needsRerender = true;
        sceneSizeChanged = true;

        renderTime = 0;

        sizing = 1;
        shadowRepeat = 10;

        unsafe { rectOutlineCol = *ImGui.GetStyleColorVec4(ImGuiCol.Border); }

        renderAllCounter = objects.Tiles.Count;
        renderAllSize = objects.Tiles.Count;

        palettes.PalettesChanged += PalettesChanged;
        objects.TilesChanged += TilesChanged;
    }

    private void PalettesChanged()
    {
        needsRerender = true;
    }

    private void TilesChanged()
    {
        scene.RemoveObject(tile);
        tile.Dispose();

        if (objects.Tiles.Count == 0)
        {
            tile = new TileObject(); // use empty cache
        }
        else if (objects.TileLastUsed == string.Empty || objects.Tiles.All(t => t.ProperName != objects.TileLastUsed))
        {
            tile = new TileObject(scene, objects, objects.Tiles[0]);
        }
        else
        {
            tile = new TileObject(scene, objects, objects.Tiles.First(t => t.ProperName == objects.TileLastUsed));
        }

        scene.AddObject(tile);
        needsRerender = true;

        renderAllCounter = objects.Tiles.Count;
        renderAllSize = objects.Tiles.Count;
    }

    private void ReRender()
    {
        Stopwatch time = Stopwatch.StartNew();

        tile.RenderVariation = variation;
        tile.Position.Z = float.Clamp(tile.Position.Z, 0, tile.HasSpecs2 ? 1 : 2);

        if (sceneSizeChanged)
        {
            camera.Resize(tile);
        }

        camera.RainPercentage = objects.TileUseRain / 100;
        camera.Unlit = objects.TileUseUnlit;
        scene.Render();

        needsRerender = false;
        sceneSizeChanged = false;

        renderTime = time.ElapsedMilliseconds;
    }

    public override void Update()
    {
        if (needsRerender) ReRender();

        base.Update();
    }

    protected override void Draw()
    {
        if (!Begin()) return;

        if (objects.Tiles.Count == 0)
        {
            ImGui.Text("NO TILES FOUND!");
            ImGui.End();
            return;
        }

        ImGui.TextDisabled(objects.GraphicsDir);
        ImGui.TextDisabled(scene.PaletteManager.CurrentPalette.Name);

        ImGui.SeparatorText("PARAMETERS");

        if (ImGui.InputTextWithHint("Search", "Type Words To Search", ref objects.TileLastSearched, 280) || searchedTiles == null)
        {
            searchedTiles = objects.Tiles.Where(t => t.SearchName.Contains(objects.TileLastSearched, StringComparison.CurrentCultureIgnoreCase));
        }

        if (ImGui.BeginCombo("##tile_picker", tile.ProperName) && searchedTiles != null)
        {
            foreach (TileInfo t in searchedTiles)
            {
                Utils.PushStyleColor(ImGuiCol.Text, t.CategoryColor);
                ImGui.Text("|");
                ImGui.SameLine();
                ImGui.PopStyleColor();

                if (t.HasWarnings)
                {
                    Utils.WarningMarker(t.Warnings);
                    ImGui.SameLine();
                }

                if (!ImGui.Selectable(t.ProperName, t.ProperName == tile.ProperName)) continue;
                if (!File.Exists(Path.Combine(objects.GraphicsDir, $"{t.Name}.png"))) continue;

                // Remove and dispose of the old cache
                scene.RemoveObject(tile);
                tile.Dispose();

                tile = new TileObject(scene, objects, t);

                // Add new cache to scene
                scene.AddObject(tile);
                needsRerender = true;
                objects.TileLastUsed = t.ProperName;
            }

            ImGui.EndCombo();
        }

        int vars = variation;
        if (tile.Variants > 1)
        {
            ImGui.SliderInt("Variant", ref variation, 0, tile.Variants - 1);
        }

        string bgName = background switch
        {
            CameraBgState.Clear => "Clear",
            CameraBgState.Sky => "Sky",
            CameraBgState.White => "White",
            CameraBgState.Custom => "Custom",
            _ => "UNKNOWN"
        };

        if (ImGui.BeginCombo("Background", bgName))
        {
            if (ImGui.Selectable("Clear"))
            {
                background = CameraBgState.Clear;
                camera.SetBackground(background);
                needsRerender = true;
            }

            if (ImGui.Selectable("Sky"))
            {
                background = CameraBgState.Sky;
                camera.SetBackground(background);
                needsRerender = true;
            }

            if (ImGui.Selectable("White"))
            {
                background = CameraBgState.White;
                camera.SetBackground(background);
                needsRerender = true;
            }

            if (ImGui.Selectable("Custom"))
            {
                background = CameraBgState.Custom;
                needsRerender = true;
            }

            ImGui.EndCombo();
        }

        if (background == CameraBgState.Custom)
        {
            Vector4 col = camera.BackgroundColor.ToVector4();
            if (ImGui.ColorEdit4("Custom Background", ref col))
            {
                camera.SetBackground(new RgbaFloat(col));
                needsRerender = true;
            }
        }

        int ly = (int)tile.Position.Z;
        int lyCheck = ly;
        ImGui.SliderInt("Layer", ref ly, 0, tile.HasSpecs2 ? 1 : 2);
        tile.Position.Z = ly;

        int sr = shadowRepeat;
        ImGui.SliderInt("Shadow Repeat", ref shadowRepeat, 1, 40);

        if (ImGui.SliderFloat("Use Rain Palette", ref objects.TileUseRain, 0, 100)) needsRerender = true;

        if (ImGui.Checkbox("Use Unlit Palette", ref objects.TileUseUnlit)) needsRerender = true;
        Vector2Int rSize = tile.GetRenderSize(camera);
        if (rSize.X != camera.Width || rSize.Y != camera.Height)
        {
            sceneSizeChanged = true;
            needsRerender = true;
        }

        if (vars != variation || lyCheck != ly || sr != shadowRepeat) needsRerender = true;

        ImGui.SeparatorText("RENDER");

        if (ImGui.TreeNode("Render All"))
        {
            ImGui.TextDisabled("Warning: Render All disregards variant and given depth");
            ImGui.TextDisabled("Bulk renders are placed in GraphicsDir/Rendered/Bulk/");
            if (renderAllSize != 0 && renderAllCounter == renderAllSize)
            {
                if (ImGui.Button("Render All"))
                {
                    Directory.CreateDirectory($"{objects.GraphicsDir}/Rendered/Bulk/");

                    renderAllCounter = 0;
                    renderAllSize = 0;
                    ThreadPool.QueueUserWorkItem(RenderAll);
                }
            }
            else
            {
                ImGui.Text(renderAllSize == 0 ? "Caching textures..." : "Rendering...");
                int renderAllAppropriateSize = renderAllSize == 0 ? objects.Tiles.Count : renderAllSize;
                ImGui.ProgressBar((float)renderAllCounter / renderAllAppropriateSize, Vector2.Zero,  $"{renderAllCounter}/{renderAllAppropriateSize}");
            }

            ImGui.TreePop();
        }

        if (ImGui.Button("Reset sizing"))
        {
            sizing = 1;
        }

        ImGui.SameLine();

        ImGui.SliderFloat("Render sizing", ref sizing, 0, 8);

        if (ImGui.Button("Save as Image"))
        {
            Directory.CreateDirectory($"{objects.GraphicsDir}/Rendered/");
            camera.SaveToFile($"{objects.GraphicsDir}/Rendered/{tile.Name}.png");
        }

        ImGui.Image(camera.ColorPass.RenderTexture.Index, camera.ColorPass.RenderTexture.Size * sizing, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);
        ImGui.SameLine();
        ImGui.Image(camera.LightingPass.RenderTexture.Index, camera.LightingPass.RenderTexture.Size * sizing, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);

        ImGui.TextDisabled($"{renderTime} ms");

        ImGui.End();
    }

    private void RenderAll(object? callback)
    {
        using Scene renderAllScene = new();
        using Camera renderAllCamera = new(renderAllScene);

        scene.AddObject(camera);
        scene.SetActiveCamera(camera);

        camera.RainPercentage = objects.TileUseRain;
        camera.SetBackground(background);
        camera.Unlit = objects.TileUseUnlit;
        camera.RainPercentage = objects.TileUseRain;

        TileObject[] tiles = objects.Tiles.Where(t => File.Exists(Path.Combine(objects.GraphicsDir, $"{t.Name}.png"))).Select(t => new TileObject(renderAllScene, objects, t)).ToArray();
        renderAllSize = tiles.Length;

        foreach (TileObject t in tiles)
        {
            renderAllCamera.Resize(t);

            renderAllScene.AddObject(t);

            renderAllScene.Render();
            GuiManager.GraphicsDevice.WaitForIdle();
            renderAllCamera.SaveToFile($"{objects.GraphicsDir}/Rendered/Bulk/{t.Name}.png");

            renderAllScene.RemoveObject(t);

            renderAllCounter++;
        }

        GuiManager.GraphicsDevice.WaitForIdle();

        foreach (TileObject o in tiles)
        {
            o.Dispose();
        }
    }

    protected override void Destroy()
    {
        scene.Dispose();
        tile.Dispose();

        palettes.PalettesChanged -= PalettesChanged;
        objects.TilesChanged -= TilesChanged;
    }
}
