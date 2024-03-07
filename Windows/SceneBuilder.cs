using System;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;
using RWBaker.RWObjects;

namespace RWBaker.Windows;

public class SceneBuilder : Window
{
    private readonly Scene _scene;
    private Vector2 _sceneSize;
    private IInspectable? _inspecting;
    private IRenderable? _inspectingRenderable;
    private SceneObject? _inspectingObject;

    private bool needsResize = true;
    private bool viewShadows = false;
    private Vector2 currentCanvasSize = Vector2.One;
    private readonly Vector4 rectOutlineCol;

    public SceneBuilder() : base("Scene Builder", "_sceneBuilder")
    {
        _scene = new Scene();
        _inspecting = _scene;

        unsafe { rectOutlineCol = *ImGui.GetStyleColorVec4(ImGuiCol.Border); }
    }

    public override void Update()
    {
        Reload();

        base.Update();
    }

    private void Reload()
    {
        if (needsResize)
        {
            _scene.Resize(currentCanvasSize);
            needsResize = false;
        }

        _scene.Render();
    }

    protected override void Draw()
    {
        Begin();

        _sceneSize.X = ImGui.GetWindowSize().X * 2f / 3f;
        float availY = ImGui.GetContentRegionAvail().Y;

        GuiTexture texture = viewShadows ? _scene.ShadowRender : _scene.ObjectRender;
        ImGui.Image(texture.Index, texture.Size, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);

        if (currentCanvasSize.X < _sceneSize.X || currentCanvasSize.X > _sceneSize.X || currentCanvasSize.Y < availY || currentCanvasSize.Y > availY)
        {
            currentCanvasSize = _sceneSize with { Y = availY };
            needsResize = true;
        }

        ImGui.SameLine();

        availY = ImGui.GetContentRegionAvail().Y / 2;
        Vector2 finalChildPos = ImGui.GetCursorScreenPos();
        finalChildPos.Y += availY + ImGui.GetStyle().WindowPadding.Y;
        if (ImGui.BeginChild("Scene items", Vector2.UnitY * availY, ImGuiChildFlags.Border))
        {
            ImGui.SeparatorText("HIERARCHY");

            if (Utils.SelectableTreeNode("Scene")) SetActiveInspect(_scene);

            foreach (SceneObject sceneObject in _scene.Objects)
            {
                if (!Utils.SelectableTreeNode(sceneObject.NameInScene)) continue;
                SetActiveInspect(sceneObject);
            }

            ImGui.EndChild();
        }

        ImGui.SetCursorScreenPos(finalChildPos);
        if (ImGui.BeginChild("Object Inspector", Vector2.Zero, ImGuiChildFlags.Border))
        {
            if (_inspecting != null)
            {
                ImGui.SeparatorText(_inspectingObject == null ? _scene.Name : _inspectingObject.NameInScene);
                _inspecting?.RenderInspector();
            }

            if (_inspectingObject != null)
            {
                ImGui.SeparatorText("In-Scene Controls");

                if (ImGui.Button("Move Up In Hierarchy")) IncreaseObjectPriority();

                if (ImGui.Button("Move Down In Hierarchy")) DecreaseObjectPriority();

                if (ImGui.Button("Remove"))
                {
                    _inspecting = _scene;
                    _scene.RemoveObject(_inspectingObject);

                    _inspectingObject = null;

                    if (_inspectingObject is IDisposable disposable) disposable.Dispose();
                }
            }

            ImGui.EndChild();
        }

        ImGui.End();
    }

    private void SetActiveInspect(object inspectable)
    {
        _inspecting = inspectable as IInspectable; // automatically null if not inspectable
        _inspectingObject = inspectable as SceneObject; // automatically null if not S.O.
        _inspectingRenderable = inspectable as IRenderable; // automatically null if not renderable
    }

    private void IncreaseObjectPriority()
    {
        if (_inspectingObject == null) return;
        _scene.Objects.ChangePriority(_inspectingObject, -1);

        if (_inspectingRenderable == null) return;
        _scene.Renderables.ChangePriority(_inspectingRenderable, -1);
    }

    private void DecreaseObjectPriority()
    {
        if (_inspectingObject == null) return;
        _scene.Objects.ChangePriority(_inspectingObject, 1);

        if (_inspectingRenderable == null) return;
        _scene.Renderables.ChangePriority(_inspectingRenderable, 1);
    }

    protected override void Destroy()
    {
        _scene.Dispose();
    }
}