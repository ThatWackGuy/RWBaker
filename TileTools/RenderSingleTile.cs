using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using RWBaker.GeneralTools;
using RWBaker.GraphicsTools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace RWBaker.TileTools;

public class RenderSingleTiles : Window
{
    private readonly Context context;
    
    private Vector2 renderOffset;
    private Vector2 perRenderOffset => (renderOffset / 100) * (Vector2)tile.PixelSize;
    private Vector2 lightOffset;
    private Vector2 perLightOffset => (lightOffset / 100) * (Vector2)tile.PixelSize;

    private readonly RWScene scene;
    private Tile tile;
    private int variation;
    private int background;
    private GuiTexture render;
    private GuiTexture shadowRender;
    private bool needsRerender;
    private long renderTime;
    private float sizing;

    private readonly Vector4 rectOutlineCol;
    
    public RenderSingleTiles() : base("Render Tile", "render_tile")
    {
        Open = true;
    
        context = Context.GetContext();
    
        renderOffset = Vector2.Zero;
        lightOffset = Vector2.Zero;

        if (context.TileLastUsed == "" || Program.Tiles.All(t => t.ProperName != context.TileLastUsed))
        {
            tile = Program.Tiles[0];
        }
        else
        {
            tile = Program.Tiles.First(t => t.ProperName == context.TileLastUsed);
        }
    
        scene = new RWScene();
        scene.ResizeTo(tile);
    
        tile.CachedTexture = Graphics.TextureFromImage(context.SavedGraphicsDir + "/" + tile.Name + ".png");

        variation = 0;

        needsRerender = true;
    
        renderTime = 0;

        sizing = 1;

        unsafe { rectOutlineCol = *ImGui.GetStyleColorVec4(ImGuiCol.Border); }
    }

    public override void Update()
    {
        if (needsRerender)
        {
            if (!File.Exists($"{context.SavedGraphicsDir}/{tile.Name}.png")) return;

            Stopwatch time = Stopwatch.StartNew();

            tile.Variation(variation);
            tile.UseRainPalette = context.TileUseRain;
            
            scene.SetBackground(background, context.TileUseRain);
            scene.AddObject(tile);
            scene.Render(context.TileUseUnlit);

            if (render == null || render.Texture != scene.ColorRenderTarget)
            {
                render?.Dispose();
                render = GuiTexture.Create(tile.FullName, scene.ColorRenderTarget);
            }

            if (shadowRender == null || shadowRender.Texture != scene.ShadowRenderTarget)
            {
                shadowRender?.Dispose();
                shadowRender = GuiTexture.Create(tile.FullName + "_SH", scene.ShadowRenderTarget);
            }

            needsRerender = false;

            renderTime = time.ElapsedMilliseconds;
        }

        base.Update();
    }

    protected override void Draw()
    {
        Begin();
        ImGui.TextDisabled($"Using Graphics Dir '{context.SavedGraphicsDir}'");
        ImGui.TextDisabled($"Using Palette: {Palette.Current.Name}");
        ImGui.Separator();

        if (Program.Tiles.Count == 0)
        {
            ImGui.Text("NO TILES FOUND!");
            ImGui.End();
            return;
        }
        
        ImGui.SliderFloat("Render sizing multiplier", ref sizing, 0, 8);

        ImGui.Separator();

        ImGui.InputTextWithHint("Search", "Type Words To Search", ref context.TileLastSearched, 280);
        
        if (ImGui.BeginCombo("##tile_picker", tile.ProperName))
        {
            foreach (Tile t in Program.Tiles.Where(t => t.ProperName.Contains(context.TileLastSearched)))
            {
                // TODO: Add category color?
                if (ImGui.Selectable(t.WarningGenerated ? $"[!] {t.ProperName}" : t.ProperName))
                {
                    tile.CachedTexture?.Dispose(); // Dispose of the last tile's texture
                    tile = t;
                    tile.CachedTexture = Graphics.TextureFromImage(context.SavedGraphicsDir + "/" + tile.Name + ".png");
                    needsRerender = true;
                    context.TileLastUsed = tile.ProperName;
                }

                if (!t.WarningGenerated) continue;
                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.TextDisabled($"Warnings have been generated:\n{t.Warnings}");
                ImGui.EndTooltip();
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
        
        if (ImGui.Checkbox("Use Unlit Palette", ref context.TileUseUnlit)) needsRerender = true;
        if (ImGui.Checkbox("Use Rain Palette", ref context.TileUseRain)) needsRerender = true;
        
        Vector2Int rSize = tile.GetRenderSize(scene);
        if (rSize.X != scene.Width || rSize.Y != scene.Height)
        {
            scene.ResizeTo(tile);
            needsRerender = true;
        }

        if (perRenderOffset != scene.ObjectOffset || perLightOffset != scene.LightOffset)
        {
            scene.ObjectOffset = perRenderOffset;
            scene.LightOffset = perLightOffset;
            
            needsRerender = true;
        }

        if (bg != background || vars != variation) needsRerender = true;

        ImGui.Separator();

        ImGui.Image(render.Index, render.Size * sizing, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);
        ImGui.Image(shadowRender.Index, shadowRender.Size * sizing, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);
        
        ImGui.TextDisabled($"RENDER TIME: {renderTime} ms.");
        
        if (ImGui.Button("Save as Image"))
        {
            Directory.CreateDirectory(context.SavedGraphicsDir + "/RENDERED/");

            scene.ResizeTo(tile);
            scene.AddObject(tile);
            scene.Render(context.TileUseUnlit);

            scene.SaveToFile($"{context.SavedGraphicsDir}/RENDERED/{tile.Name}.png");
        }
        
        ImGui.End();
    }
}