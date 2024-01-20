using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;

namespace RWBaker.TileTools;

public class RenderSingleTiles : Window
{
    private Vector2 renderOffset;
    private Vector2 perRenderOffset => (renderOffset / 100) * (Vector2)tile.PixelSize;
    private Vector2 lightOffset;
    private Vector2 perLightOffset => (lightOffset / 100) * (Vector2)tile.PixelSize;

    private readonly RWScene scene;
    private Tile tile;
    private IEnumerable<Tile>? searchedTiles;
    private int variation;
    private int background;
    private int layer;
    private bool needsRerender;
    private bool sceneSizeChanged;
    private long renderTime;
    private float sizing;
    private int shadowRepeat;

    private readonly Vector4 rectOutlineCol;
    
    public RenderSingleTiles() : base("Render Tile", "render_tile")
    {
        renderOffset = Vector2.Zero;
        lightOffset = Vector2.Zero;

        if (context.TileLastUsed == string.Empty || Program.Tiles.All(t => t.ProperName != context.TileLastUsed))
        {
            tile = Program.Tiles[0];
        }
        else
        {
            tile = Program.Tiles.First(t => t.ProperName == context.TileLastUsed);
        }

        searchedTiles = null;
    
        scene = new RWScene();

        tile.CacheTexture(context);

        variation = 0;

        needsRerender = true;
        sceneSizeChanged = true;
    
        renderTime = 0;

        sizing = 1;
        shadowRepeat = 10;

        unsafe { rectOutlineCol = *ImGui.GetStyleColorVec4(ImGuiCol.Border); }
    }

    private void ReRender()
    {
        if (tile.CachedTexture is null) return;
            
        Stopwatch time = Stopwatch.StartNew();

        tile.RenderVariation = variation;
        tile.UseRainPalette = context.TileUseRain;
        tile.RenderLayer = layer;

        if (sceneSizeChanged)
        {
            scene.Resize(tile);
        }

        scene.ShadowRepeat = shadowRepeat;
        scene.SetBackground(background, context.TileUseRain);
        scene.AddObject(tile);
        scene.Render(context.TileUseUnlit);

        needsRerender = false;
        sceneSizeChanged = false;

        renderTime = time.ElapsedMilliseconds;
    }
    
    public override void Update()
    {
        if (needsRerender || PaletteManager.CurrentChanged) ReRender();

        base.Update();
    }

    protected override void Draw()
    {
        Begin();
        
        if (Program.Tiles.Count == 0)
        {
            ImGui.Text("NO TILES FOUND!");
            ImGui.End();
            return;
        }
        
        ImGui.TextDisabled(context.SavedGraphicsDir);
        ImGui.TextDisabled(PaletteManager.CurrentPalette.Name);
        
        ImGui.SeparatorText("PARAMETERS");
        
        if (ImGui.InputTextWithHint("Search", "Type Words To Search", ref context.TileLastSearched, 280) || searchedTiles == null)
        {
            searchedTiles = Program.Tiles.Where(t => t.SearchName.Contains(context.TileLastSearched, StringComparison.CurrentCultureIgnoreCase));
        }
        
        if (ImGui.BeginCombo("##tile_picker", tile.ProperName) && searchedTiles != null)
        {
            foreach (Tile t in searchedTiles)
            {
                Utils.PushStyleColor(ImGuiCol.Text, t.CategoryColor);
                ImGui.Text("|");
                ImGui.SameLine();
                ImGui.PopStyleColor();
                
                if (t.WarningGenerated)
                {
                    Utils.WarningMarker(t.Warnings);
                    ImGui.SameLine();
                }
                
                if (ImGui.Selectable(t.ProperName, t.ProperName == tile.ProperName))
                {
                    tile.DisposeTexture(); // Dispose of the last tile's texture
                    tile = t;
                    tile.CacheTexture(context);
                    needsRerender = true;
                    context.TileLastUsed = tile.ProperName;
                }
            }

            ImGui.EndCombo();
        }
        
        if (tile.CachedTexture is null)
        {
            ImGui.Text("TILE TEXTURE DOES NOT EXIST!");
            ImGui.End();
            return;
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

        int ly = layer;
        ImGui.SliderInt("Layer", ref layer, 0, tile.HasSpecs2 ? 1 : 2);
        
        ImGui.Spacing();
        ImGui.Spacing();
        
        int sr = shadowRepeat;
        ImGui.SliderInt("Shadow Repeat", ref shadowRepeat, 1, 40);
        
        ImGui.Spacing();
        ImGui.Spacing();
        
        if (ImGui.Checkbox("Use Unlit Palette", ref context.TileUseUnlit)) needsRerender = true;
        if (ImGui.Checkbox("Use Rain Palette", ref context.TileUseRain)) needsRerender = true;
        
        Vector2Int rSize = tile.GetRenderSize(scene);
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
            Directory.CreateDirectory($"{context.SavedGraphicsDir}/Rendered/");
            scene.SaveToFile($"{context.SavedGraphicsDir}/Rendered/{tile.Name}.png");
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
        tile.CachedTexture?.Dispose();
    }
}