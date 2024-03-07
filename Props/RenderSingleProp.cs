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

namespace RWBaker.Props;

public class RenderSingleProp : Window
{
    private RWObjectManager objects => Program.ObjectManager;
    private PaletteManager palettes => Program.PaletteManager;

    private readonly Scene scene;

    private PropObject prop;
    private IEnumerable<IProp>? searchedProps;
    private int variation;
    private int background;
    private int layer;
    private bool needsRerender;
    private bool sceneSizeChanged;
    private long renderTime;
    private float sizing;
    private int shadowRepeat;

    private Vector2 renderOffset;
    private Vector2 perRenderOffset => (renderOffset / 100) * (Vector2)prop.PixelSize;
    private Vector2 lightOffset;
    private Vector2 perLightOffset => (lightOffset / 100) * (Vector2)prop.PixelSize;

    private readonly Vector4 rectOutlineCol;

    public RenderSingleProp() : base("Render Prop", "_renderProp")
    {
        scene = new Scene();

        renderOffset = Vector2.Zero;
        lightOffset = Vector2.Zero;

        if (objects.Props.Count == 0)
        {
            prop = new PropObject(); // use empty cache
        }
        else if (objects.PropLastUsed == string.Empty || objects.Props.All(t => t.ProperName() != objects.PropLastUsed))
        {
            prop = new PropObject(scene, objects, objects.Props[0]);
        }
        else
        {
            prop = new PropObject(scene, objects, objects.Props.First(t => t.ProperName() == objects.PropLastUsed));
        }

        scene.AddObject(prop);

        searchedProps = null;

        variation = 0;

        needsRerender = true;
        sceneSizeChanged = true;

        renderTime = 0;

        sizing = 1;
        shadowRepeat = 10;

        unsafe { rectOutlineCol = *ImGui.GetStyleColorVec4(ImGuiCol.Border); }

        palettes.PalettesChanged += PalettesChanged;
        objects.PropsChanged += PropsChanged;
    }

    private void PalettesChanged()
    {
        needsRerender = true;
    }

    private void PropsChanged()
    {
        scene.RemoveObject(prop);
        prop.Dispose();

        if (objects.Props.Count == 0)
        {
            prop = new PropObject(); // use empty cache
        }
        else if (objects.PropLastUsed == string.Empty || objects.Props.All(t => t.ProperName() != objects.PropLastUsed))
        {
            prop = new PropObject(scene, objects, objects.Props[0]);
        }
        else
        {
            prop = new PropObject(scene, objects, objects.Props.First(t => t.ProperName() == objects.PropLastUsed));
        }

        scene.AddObject(prop);

        needsRerender = true;
    }

    private void ReRender()
    {
        Stopwatch time = Stopwatch.StartNew();

        prop.RenderVariation = variation;

        if (sceneSizeChanged)
        {
            scene.Resize(prop);
        }

        scene.ShadowRepeat = shadowRepeat;
        scene.Rain = objects.PropUseRain;
        scene.Rain = objects.PropUseRain;
        scene.Unlit = objects.PropUseUnlit;

        scene.SetBackground(background);
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

        if (objects.Props.Count == 0)
        {
            ImGui.Text("NO PROPS FOUND!");
            ImGui.End();
            return;
        }

        ImGui.TextDisabled(objects.PropsDir);
        ImGui.TextDisabled(scene.PaletteManager.CurrentPalette.Name);

        ImGui.SeparatorText("PARAMETERS");

        if (ImGui.InputTextWithHint("Search", "Type Words To Search", ref objects.PropLastSearched, 280) || searchedProps == null)
        {
            searchedProps = objects.Props.Where(t => t.SearchName().Contains(objects.PropLastSearched, StringComparison.CurrentCultureIgnoreCase));
        }

        if (ImGui.BeginCombo("##prop_picker", prop.ProperName) && searchedProps != null)
        {
            foreach (IProp t in searchedProps)
            {
                Utils.PushStyleColor(ImGuiCol.Text, t.CategoryColor());
                ImGui.Text("|");
                ImGui.SameLine();
                ImGui.PopStyleColor();

                if (t.HasWarnings())
                {
                    Utils.WarningMarker(t.Warnings());
                    ImGui.SameLine();
                }

                if (!ImGui.Selectable(t.ProperName(), t.ProperName() == prop.ProperName)) continue;
                if (!File.Exists(Path.Combine(objects.PropsDir, $"{t.Name()}.png"))) continue;

                // Remove and dispose of the old cache
                scene.RemoveObject(prop);
                prop.Dispose();

                prop = new PropObject(scene, objects, t);

                // Add new cache to scene
                scene.AddObject(prop);
                needsRerender = true;
                objects.PropLastUsed = t.ProperName();
            }

            ImGui.EndCombo();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        int vars = variation;
        if (prop.Variants > 1)
        {
            ImGui.SliderInt("Variant", ref variation, 0, prop.Variants - 1);
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

        int ly = (int)prop.Position.Z;
        ImGui.SliderInt("Sublayer", ref ly, 0, 29 - prop.Depth);
        prop.Position.Z = int.Clamp(ly, 0, 30 - prop.Depth);

        ImGui.Spacing();
        ImGui.Spacing();

        int sr = shadowRepeat;
        ImGui.SliderInt("Shadow Repeat", ref shadowRepeat, 1, 40);

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.Checkbox("Use Unlit Palette", ref objects.PropUseUnlit)) needsRerender = true;
        if (ImGui.Checkbox("Use Rain Palette", ref objects.PropUseRain)) needsRerender = true;

        Vector2Int rSize = prop.GetRenderSize(scene);
        if (rSize.X != scene.Width || rSize.Y != scene.Height)
        {
            sceneSizeChanged = true;
            needsRerender = true;
        }

        if (perRenderOffset != scene.ObjectOffset || perLightOffset != scene.LightAngle)
        {
            scene.ObjectOffset = perRenderOffset;
            scene.LightAngle = perLightOffset;

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
            Directory.CreateDirectory($"{objects.PropsDir}/Rendered/");
            scene.SaveToFile($"{objects.PropsDir}/Rendered/{prop.Name}.png");
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
        prop.Dispose();

        palettes.PalettesChanged -= PalettesChanged;
        objects.PropsChanged -= PropsChanged;
    }
}