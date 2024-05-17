using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Palettes;
using RWBaker.Props;
using RWBaker.Tiles;
using Veldrid;

namespace RWBaker.Rendering;

public class Scene : IInspectable, IDisposable
{
    public RWObjectManager ObjectManager => Program.ObjectManager;
    public PaletteManager PaletteManager => Program.PaletteManager;


    private readonly GraphicsDevice graphicsDevice;

    public readonly CommandList CommandList;

    public List<SceneObject> Objects { get; private set; } = new();
    public List<IRenderable> Renderables { get; private set; } = new();

    public Camera? ActiveCamera;
    public List<Camera> Cameras { get; private set; } = new();

    public Scene()
    {

        graphicsDevice = GuiManager.GraphicsDevice;

        CommandList = graphicsDevice.ResourceFactory.CreateCommandList();
    }

    public void RenderInspector()
    {
        if (ImGui.Button("Add Tile")) ImGui.OpenPopup("addTile");

        if (ImGui.Button("Add Prop")) ImGui.OpenPopup("addProp");

        if (ImGui.BeginPopup("addTile"))
        {
            ImGui.InputTextWithHint("Search", "Type Words To Search", ref ObjectManager.TileLastSearched, 280);

            if (ImGui.BeginCombo("##tile_picker", "Select Tile to Add"))
            {
                foreach (TileInfo t in ObjectManager.Tiles.Where(t => t.SearchName.Contains(ObjectManager.TileLastSearched, StringComparison.CurrentCultureIgnoreCase)))
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

                    if (!ImGui.Selectable(t.ProperName)) continue;
                    if (!File.Exists(Path.Combine(ObjectManager.GraphicsDir, $"{t.Name}.png"))) continue;
                    AddObject(new TileObject(this, ObjectManager, t));
                    ObjectManager.TileLastUsed = t.ProperName;
                }

                ImGui.EndCombo();
            }

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopup("addProp"))
        {
            ImGui.InputTextWithHint("Search", "Type Words To Search", ref ObjectManager.PropLastSearched, 280);
            if (ImGui.BeginCombo("##prop_picker", "Select Prop To Add"))
            {
                foreach (Prop p in ObjectManager.Props.Where(t => t.SearchName.Contains(ObjectManager.PropLastSearched, StringComparison.CurrentCultureIgnoreCase)))
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

                    if (!ImGui.Selectable(p.ProperName)) continue;
                    if (!File.Exists(Path.Combine(ObjectManager.PropsDir, $"{p.Name}.png"))) continue;
                   AddObject(p.AsObject(this, ObjectManager));
                   ObjectManager.PropLastUsed = p.ProperName;
                }

                ImGui.EndCombo();
            }

            ImGui.EndPopup();
        }
    }

    public void AddObject(SceneObject sceneObject)
    {
        Objects.Add(sceneObject);
        if (sceneObject is Camera camera) Cameras.Add(camera);
        if (sceneObject is IRenderable renderable) Renderables.Add(renderable);
    }

    public void RemoveObject(SceneObject sceneObject)
    {
        Objects.Remove(sceneObject);
        if (sceneObject is Camera camera) Cameras.Remove(camera);
        if (sceneObject is IRenderable renderable) Renderables.Remove(renderable);
    }

    public void SetActiveCamera(Camera camera)
    {
        ActiveCamera = camera;
    }

    /**
     * Renders all <see cref="Camera"/>s in Scene.<see cref="Cameras"/>
     */
    public void Render()
    {
        CommandList.Begin();

        foreach (Camera camera in Cameras)
        {
            var activeRenderData = Renderables.Select(r => r.GetRenderDescription(camera));

            camera.Render(activeRenderData);
        }

        CommandList.End();

        // Submit and wait for completion
        graphicsDevice.SubmitCommands(CommandList);
        graphicsDevice.WaitForIdle();
    }

    public void Dispose()
    {
        graphicsDevice.WaitForIdle();

        Objects.Clear();

        CommandList.Dispose();

        GC.SuppressFinalize(this);
    }
}