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

namespace RWBaker.Props;

public class RenderProps : Window
{
    private RWObjectManager objects => Program.ObjectManager;
    private PaletteManager palettes => Program.PaletteManager;

    private readonly Scene scene;
    private readonly Camera camera;

    private PropObject prop;
    private IEnumerable<Prop>? searchedProps;
    private int variation;
    private CameraBgState background;
    private bool needsRerender;
    private bool sceneSizeChanged;
    private long renderTime;
    private float sizing;
    private int shadowRepeat;

    private readonly Vector4 rectOutlineCol;

    private int renderAllSize;
    private int renderAllCounter;

    public RenderProps() : base("Render Prop", "_renderProp")
    {
        scene = new Scene();
        camera = new Camera(scene);

        scene.AddObject(camera);
        scene.SetActiveCamera(camera);

        if (objects.Props.Count == 0)
        {
            prop = new PropObject(); // use empty cache
        }
        else if (objects.PropLastUsed == string.Empty || objects.Props.All(t => t.ProperName != objects.PropLastUsed))
        {
            prop = objects.Props[0].AsObject(scene, objects);
        }
        else
        {
            prop = objects.Props.First(t => t.ProperName == objects.PropLastUsed).AsObject(scene, objects);
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

        renderAllSize = objects.Props.Count;
        renderAllCounter = objects.Props.Count;

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
        else if (objects.PropLastUsed == string.Empty || objects.Props.All(t => t.ProperName != objects.PropLastUsed))
        {
            prop = objects.Props[0].AsObject(scene, objects);
        }
        else
        {
            prop = objects.Props.First(t => t.ProperName == objects.PropLastUsed).AsObject(scene, objects);
        }

        scene.AddObject(prop);

        renderAllSize = objects.Props.Count;
        renderAllCounter = objects.Props.Count;

        needsRerender = true;
    }

    private void ReRender()
    {
        Stopwatch time = Stopwatch.StartNew();

        prop.RenderVariation = variation;

        if (sceneSizeChanged)
        {
            camera.Resize(prop);
        }

        camera.RainPercentage = objects.PropUseRain / 100;
        camera.Unlit = objects.PropUseUnlit;

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
            searchedProps = objects.Props.Where(t => t.SearchName.Contains(objects.PropLastSearched, StringComparison.CurrentCultureIgnoreCase));
        }

        if (ImGui.BeginCombo("##prop_picker", prop.OriginalName) && searchedProps != null)
        {
            foreach (Prop p in searchedProps)
            {
                Utils.PushStyleColor(ImGuiCol.Text, p.CategoryColor);
                ImGui.Text("|");
                ImGui.SameLine();
                ImGui.PopStyleColor();

                if (p.HasWarnings)
                {
                    Utils.WarningMarker(p.Warnings);
                    ImGui.SameLine();
                }

                if (!ImGui.Selectable(p.ProperName, p.ProperName == prop.OriginalName)) continue;
                if (!File.Exists(Path.Combine(objects.PropsDir, $"{p.Name}.png"))) continue;

                // Remove and dispose of the old cache
                scene.RemoveObject(prop);
                prop.Dispose();

                prop = p.AsObject(scene, objects);

                // Add new cache to scene
                scene.AddObject(prop);
                needsRerender = true;
                objects.PropLastUsed = p.ProperName;
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

        int ly = (int)prop.Position.Z;
        int lyCheck = ly;
        ImGui.SliderInt("Sublayer", ref ly, 0, 29 - prop.Depth);
        prop.Position.Z = int.Clamp(ly, 0, 30 - prop.Depth);

        ImGui.Spacing();
        ImGui.Spacing();

        int sr = shadowRepeat;
        ImGui.SliderInt("Shadow Repeat", ref shadowRepeat, 1, 40);

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.SliderFloat("Use Rain Palette", ref objects.PropUseRain, 0, 100)) needsRerender = true;

        if (ImGui.Checkbox("Use Unlit Palette", ref objects.PropUseUnlit)) needsRerender = true;

        Vector2Int rSize = prop.GetRenderSize(camera);
        if (rSize.X != camera.Width || rSize.Y != camera.Height)
        {
            sceneSizeChanged = true;
            needsRerender = true;
        }

        if (vars != variation || ly != lyCheck || sr != shadowRepeat) needsRerender = true;

        ImGui.SeparatorText("RENDER");

        if (ImGui.TreeNode("Render All"))
        {
            ImGui.TextDisabled("Warning: Render All disregards variant and given depth");
            ImGui.TextDisabled("Bulk renders are placed in PropsDir/Rendered/Bulk/");
            if (renderAllCounter == renderAllSize)
            {
                if (ImGui.Button("Render All"))
                {
                    Directory.CreateDirectory($"{objects.PropsDir}/Rendered/Bulk/");

                    renderAllCounter = 0;
                    renderAllSize = 0;
                    ThreadPool.QueueUserWorkItem(RenderAll);
                }
            }
            else
            {
                ImGui.Text(renderAllSize == 0 ? "Caching textures..." : "Rendering...");
                int renderAllAppropriateSize = renderAllSize == 0 ? objects.Props.Count : renderAllSize;
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
            Directory.CreateDirectory($"{objects.PropsDir}/Rendered/");
            camera.SaveToFile($"{objects.PropsDir}/Rendered/{prop.Name}.png");
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

        renderAllScene.AddObject(renderAllCamera);
        renderAllScene.SetActiveCamera(renderAllCamera);

        camera.RainPercentage = objects.TileUseRain;
        camera.SetBackground(background);
        camera.Unlit = objects.TileUseUnlit;
        camera.RainPercentage = objects.TileUseRain;

        PropObject[] props = objects.Props.Where(p => File.Exists(Path.Combine(objects.GraphicsDir, $"{p.Name}.png"))).Select(p => p.AsObject(renderAllScene, objects)).ToArray();
        renderAllSize = props.Length;

        foreach (PropObject p in props)
        {
            renderAllCamera.Resize(p);

            renderAllScene.AddObject(p);

            renderAllScene.Render();
            GuiManager.GraphicsDevice.WaitForIdle();
            renderAllCamera.SaveToFile($"{objects.GraphicsDir}/Rendered/Bulk/{p.Name}.png");

            renderAllScene.RemoveObject(p);

            renderAllCounter++;
        }

        GuiManager.GraphicsDevice.WaitForIdle();

        foreach (PropObject o in props)
        {
            o.Dispose();
        }
    }

    protected override void Destroy()
    {
        scene.Dispose();
        prop.Dispose();

        palettes.PalettesChanged -= PalettesChanged;
        objects.PropsChanged -= PropsChanged;
    }
}