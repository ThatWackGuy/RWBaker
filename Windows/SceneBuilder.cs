using System;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;

namespace RWBaker.Windows;

public class SceneBuilder : Window
{
    private readonly Scene _scene;
    private IInspectable? _inspecting;
    private IRenderable? _inspectingRenderable;
    private ISceneEditable? _inspectingEditable;
    private SceneObject? _inspectingObject;

    private bool setWindowToMinimumSize;
    private bool needsResize = true;
    private int viewStencil;
    private Vector2 currentCanvasSize = Vector2.One;
    private readonly Vector4 rectOutlineCol;

    public SceneBuilder() : base("Scene Builder", "_sceneBuilder")
    {
        _scene = new Scene();
        _inspecting = _scene;

        Camera firstCamera = new(_scene);

        _scene.AddObject(firstCamera);
        _scene.SetActiveCamera(firstCamera);

        unsafe { rectOutlineCol = *ImGui.GetStyleColorVec4(ImGuiCol.Border); }
    }

    public override void Update()
    {
        if (needsResize && _scene.ActiveCamera != null)
        {
            // Make sure canvas size never reaches 0
            if (currentCanvasSize.X < 20 || currentCanvasSize.Y < 20)
            {
                setWindowToMinimumSize = true;
                currentCanvasSize = Vector2.One * 20;
            }

            _scene.ActiveCamera.Resize(currentCanvasSize);
            needsResize = false;
        }

        _scene.Render();

        base.Update();
    }

    protected override void Draw()
    {
        if (setWindowToMinimumSize)
        {
            ImGui.SetNextWindowSize(Vector2.One * 20);
            setWindowToMinimumSize = false;
        }

        if (!Begin()) return;

        Vector2 padding = ImGui.GetStyle().WindowPadding;
        float availX = float.Floor((ImGui.GetWindowSize().X * 2f / 3f) / 10f) * 10f;
        float availY = float.Floor(ImGui.GetContentRegionAvail().Y / 20f) * 20f;

        if (currentCanvasSize.X < availX || currentCanvasSize.X > availX || currentCanvasSize.Y < availY || currentCanvasSize.Y > availY)
        {
            currentCanvasSize = new Vector2(availX, availY);
            needsResize = true;
        }

        Vector2 viewMenuPos = ImGui.GetCursorPos();
        viewMenuPos.X += availX - 32 - padding.X;
        viewMenuPos.Y += padding.X;

        Vector2 renderPos = ImGui.GetCursorScreenPos();

        if (_scene.ActiveCamera != null)
        {
            GuiTexture texture = viewStencil switch
            {
                0 => _scene.ActiveCamera.ColorPass.RenderTexture,
                1 => _scene.ActiveCamera.LightingPass.RenderTexture,
                2 => _scene.ActiveCamera.RemovalPass.RenderTexture,
                _ => GuiManager.MissingTex
            };

            ImGui.Image(texture.Index, _scene.ActiveCamera.ColorPass.RenderTexture.Size, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);

            if (_inspectingEditable != null && _inspectingObject != null)
            {
                _inspectingEditable?.RenderSceneRepresentation(ImGui.GetWindowDrawList(), renderPos with
                {
                    X = renderPos.X + _inspectingObject.Position.X + _inspectingObject.Size.X / 2,
                    Y = renderPos.Y + _inspectingObject.Position.Y + _inspectingObject.Size.Y / 2
                });
            }

            ImGui.SameLine();
            Vector2 oldPos = ImGui.GetCursorPos();
            ImGui.SetCursorPos(viewMenuPos);

            // Draw view switcher
            texture = viewStencil switch
            {
                0 => _scene.ActiveCamera.LightingPass.RenderTexture,
                1 => _scene.ActiveCamera.RemovalPass.RenderTexture,
                2 => _scene.ActiveCamera.ColorPass.RenderTexture,
                _ => GuiManager.MissingTex
            };

            ImGui.Image(texture.Index, Vector2.Normalize(texture.Size) * 32, Vector2.Zero, Vector2.One, Vector4.One, rectOutlineCol);

            if (ImGui.IsItemClicked()) viewStencil = (viewStencil + 1) % 3;

            if (ImGui.IsItemHovered() && ImGui.BeginItemTooltip())
            {
                ImGui.Text("Click to switch between views.");
                ImGui.EndTooltip();
            }

            ImGui.SetCursorPos(oldPos);
        }
        else
        {
            ImGui.SameLine();
        }

        availY = ImGui.GetContentRegionAvail().Y / 2;
        Vector2 finalChildPos = ImGui.GetCursorPos();
        finalChildPos.Y += availY + padding.Y;
        if (ImGui.BeginChild("Scene Items", Vector2.UnitY * availY, ImGuiChildFlags.Border))
        {
            ImGui.SeparatorText("HIERARCHY");

            if (Utils.SelectableTreeNode("Scene")) SetActiveInspect(_scene);

            foreach (SceneObject sceneObject in _scene.Objects)
            {
                sceneObject.Update();

                if (!Utils.SelectableTreeNode(sceneObject.NameInScene)) continue;
                SetActiveInspect(sceneObject);
            }

            ImGui.EndChild();
        }

        ImGui.SetCursorPos(finalChildPos);
        if (ImGui.BeginChild("Object Inspector", Vector2.Zero, ImGuiChildFlags.Border))
        {
            if (_inspecting != null)
            {
                ImGui.SeparatorText(_inspectingObject == null ? "Scene" : _inspectingObject.NameInScene);
                _inspecting.RenderInspector();
            }

            if (_inspectingObject != null)
            {
                ImGui.SeparatorText("In-Scene Controls");

                if (ImGui.Button("Move Up In Hierarchy")) IncreaseObjectPriority();

                if (ImGui.Button("Move Down In Hierarchy")) DecreaseObjectPriority();

                if (_inspectingObject is not Camera && ImGui.Button("Remove"))
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
        _inspectingRenderable = inspectable as IRenderable; // automatically null if not renderable
        _inspectingEditable = inspectable as ISceneEditable; // automatically null if not editable
        _inspectingObject = inspectable as SceneObject; // automatically null if not S.O.
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