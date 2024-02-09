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

    private readonly RWScene scene;
    private CachedTile cachedTile;
    private IEnumerable<Tile>? searchedTiles;
    private int variation;
    private int background;
    private int layer;
    private bool needsRerender;
    private bool sceneSizeChanged;
    private long renderTime;
    private float sizing;
    private int shadowRepeat;

    private Vector2 renderOffset;
    private Vector2 perRenderOffset => (renderOffset / 100) * (Vector2)cachedTile.PixelSize;
    private Vector2 lightOffset;
    private Vector2 perLightOffset => (lightOffset / 100) * (Vector2)cachedTile.PixelSize;

    private readonly Vector4 rectOutlineCol;

    public RenderSingleTiles() : base("Render Tile", "_renderTile")
    {
        renderOffset = Vector2.Zero;
        lightOffset = Vector2.Zero;

        if (objects.Tiles.Count == 0)
        {
            cachedTile = new CachedTile(); // use empty cache
        }
        else if (objects.TileLastUsed == string.Empty || objects.Tiles.All(t => t.ProperName != objects.TileLastUsed))
        {
            cachedTile = new CachedTile(objects, objects.Tiles[0]);
        }
        else
        {
            cachedTile = new CachedTile(objects, objects.Tiles.First(t => t.ProperName == objects.TileLastUsed));
        }

        searchedTiles = null;

        scene = new RWScene();

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
        if (objects.Tiles.Count == 0)
        {
            cachedTile = new CachedTile(); // use empty cache
        }
        else if (objects.TileLastUsed == string.Empty || objects.Tiles.All(t => t.ProperName != objects.TileLastUsed))
        {
            cachedTile = new CachedTile(objects, objects.Tiles[0]);
        }
        else
        {
            cachedTile = new CachedTile(objects, objects.Tiles.First(t => t.ProperName == objects.TileLastUsed));
        }

        needsRerender = true;
    }

    private void ReRender()
    {
        Stopwatch time = Stopwatch.StartNew();

        cachedTile.RenderVariation = variation;
        cachedTile.UseRainPalette = objects.TileUseRain;
        cachedTile.RenderLayer = layer;

        if (sceneSizeChanged)
        {
            scene.Resize(cachedTile);
        }

        scene.ShadowRepeat = shadowRepeat;
        scene.SetBackground(background, objects.TileUseRain);
        scene.AddObject(cachedTile);
        scene.Render(objects.TileUseUnlit);

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
        ImGui.TextDisabled(scene.Palettes.CurrentPalette.Name);

        ImGui.SeparatorText("PARAMETERS");

        if (ImGui.InputTextWithHint("Search", "Type Words To Search", ref objects.TileLastSearched, 280) || searchedTiles == null)
        {
            searchedTiles = objects.Tiles.Where(t => t.SearchName.Contains(objects.TileLastSearched, StringComparison.CurrentCultureIgnoreCase));
        }

        if (ImGui.BeginCombo("##tile_picker", cachedTile.ProperName) && searchedTiles != null)
        {
            foreach (Tile t in searchedTiles)
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

                if (!ImGui.Selectable(t.ProperName, t.ProperName == cachedTile.ProperName)) continue;
                if (!File.Exists(Path.Combine(objects.GraphicsDir, $"{t.Name}.png"))) continue;

                cachedTile.Dispose();
                cachedTile = new CachedTile(objects, t);
                needsRerender = true;
                objects.TileLastUsed = t.ProperName;
            }

            ImGui.EndCombo();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        int vars = variation;
        if (cachedTile.Variants > 1)
        {
            ImGui.SliderInt("Variant", ref variation, 0, cachedTile.Variants - 1);
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

        int ly = layer;
        ImGui.SliderInt("Layer", ref layer, 0, cachedTile.HasSpecs2 ? 1 : 2);

        ImGui.Spacing();
        ImGui.Spacing();

        int sr = shadowRepeat;
        ImGui.SliderInt("Shadow Repeat", ref shadowRepeat, 1, 40);

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.Checkbox("Use Unlit Palette", ref objects.TileUseUnlit)) needsRerender = true;
        if (ImGui.Checkbox("Use Rain Palette", ref objects.TileUseRain)) needsRerender = true;

        Vector2Int rSize = cachedTile.GetRenderSize(scene);
        if (rSize.X != scene.Width || rSize.Y != scene.Height)
        {
            sceneSizeChanged = true;
            needsRerender = true;
        }

        if (perRenderOffset != scene.ObjectOffset || perLightOffset != scene.LightOffset)
        {
            scene.ObjectOffset = perRenderOffset;
            scene.LightOffset = perLightOffset;

            needsRerender = true;
        }

        if (bg != background || vars != variation || ly != layer || sr != shadowRepeat) needsRerender = true;

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
            scene.SaveToFile($"{objects.GraphicsDir}/Rendered/{cachedTile.Name}.png");
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
        cachedTile.Dispose();

        palettes.PalettesChanged -= PalettesChanged;
        objects.TilesChanged -= TilesChanged;
    }
}