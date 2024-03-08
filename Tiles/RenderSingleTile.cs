using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Palettes;
using RWBaker.Rendering;
using RWBaker.RWObjects;
using RWBaker.Windows;

namespace RWBaker.Tiles;

public class RenderSingleTiles : Window
{
    private RWObjectManager objects => Program.ObjectManager;
    private PaletteManager palettes => Program.PaletteManager;

    private readonly Scene scene;

    private TileObject tile;
    private IEnumerable<TileInfo>? searchedTiles;
    private int variation;
    private int background;
    private bool needsRerender;
    private bool sceneSizeChanged;
    private long renderTime;
    private float sizing;
    private int shadowRepeat;

    private Vector2 renderOffset;
    private Vector2 perRenderOffset => (renderOffset / 100) * (Vector2)tile.PixelSize;
    private Vector2 lightOffset;
    private Vector2 perLightOffset => (lightOffset / 100) * (Vector2)tile.PixelSize;

    private readonly Vector4 rectOutlineCol;

    public RenderSingleTiles() : base("Render Tile", "_renderTile")
    {
        scene = new Scene();

        renderOffset = Vector2.Zero;
        lightOffset = Vector2.Zero;

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
    }

    private void ReRender()
    {
        Stopwatch time = Stopwatch.StartNew();

        tile.RenderVariation = variation;
        tile.Position.Z = float.Clamp(tile.Position.Z, 0, tile.HasSpecs2 ? 1 : 2);

        if (sceneSizeChanged)
        {
            scene.Resize(tile);
        }

        scene.ShadowRepeat = shadowRepeat;
        scene.Rain = objects.TileUseRain;
        scene.SetBackground(background);
        scene.Unlit = objects.TileUseUnlit;
        scene.Rain = objects.TileUseRain;
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
        Begin();

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

        ImGui.Spacing();
        ImGui.Spacing();

        int vars = variation;
        if (tile.Variants > 1)
        {
            ImGui.SliderInt("Variant", ref variation, 0, tile.Variants - 1);
        }

        string bgName = background switch
        {
            0 => "Clear",
            1 => "White",
            2 => "Sky",
            _ => "UNKNOWN"
        };

        int bg = background;
        ImGui.SliderInt("Background", ref background, 0, 2, bgName);

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.SliderFloat("Layer Offset X", ref renderOffset.X, -50, 50);
        ImGui.SliderFloat("Layer Offset Y", ref renderOffset.Y, -50, 50);
        if (ImGui.Button("Reset Layer Offset")) renderOffset = Vector2.Zero;

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.SliderFloat("Light Offset X", ref lightOffset.X, -50, 50);
        ImGui.SliderFloat("Light Offset Y", ref lightOffset.Y, -50, 50);
        if (ImGui.Button("Reset Light Offset")) lightOffset = Vector2.Zero;

        ImGui.Spacing();
        ImGui.Spacing();

        int ly = (int)tile.Position.Z;
        int lyCheck = ly;
        ImGui.SliderInt("Layer", ref ly, 0, tile.HasSpecs2 ? 1 : 2);
        tile.Position.Z = ly;

        ImGui.Spacing();
        ImGui.Spacing();

        int sr = shadowRepeat;
        ImGui.SliderInt("Shadow Repeat", ref shadowRepeat, 1, 40);

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.Checkbox("Use Unlit Palette", ref objects.TileUseUnlit)) needsRerender = true;
        if (ImGui.Checkbox("Use Rain Palette", ref objects.TileUseRain)) needsRerender = true;

        Vector2Int rSize = tile.GetRenderSize(scene);
        if (rSize.X != scene.Width || rSize.Y != scene.Height || perLightOffset != scene.LightAngle)
        {
            sceneSizeChanged = true;
            needsRerender = true;
        }

        if (perRenderOffset != scene.ObjectOffset)
        {
            scene.ObjectOffset = perRenderOffset;
            scene.LightAngle = perLightOffset;

            needsRerender = true;
        }

        if (bg != background || vars != variation || lyCheck != ly || sr != shadowRepeat) needsRerender = true;

        ImGui.SeparatorText("RENDER");

        if (ImGui.Button("Reset sizing"))
        {
            sizing = 1;
        }

        ImGui.SameLine();

        ImGui.SliderFloat("Render sizing", ref sizing, 0, 8);

        if (ImGui.Button("Save as Image"))
        {
            Directory.CreateDirectory($"{objects.GraphicsDir}/Rendered/");
            scene.SaveToFile($"{objects.GraphicsDir}/Rendered/{tile.Name}.png");
        }

        ImGui.Image(scene.ObjectRender.Index, scene.ObjectRender.Size * sizing, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);
        ImGui.SameLine();
        ImGui.Image(scene.ShadowRender.Index, scene.ShadowRender.Size * sizing, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);

        ImGui.TextDisabled($"{renderTime} ms");

        ImGui.End();
    }

    protected override void Destroy()
    {
        scene.Dispose();
        tile.Dispose();

        palettes.PalettesChanged -= PalettesChanged;
        objects.TilesChanged -= TilesChanged;
    }
}