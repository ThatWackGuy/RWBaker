using System;
using System.Numerics;
using ImGuiNET;
using RWBaker.Rendering;
using SixLabors.ImageSharp;

namespace RWBaker.Gui;

public class SceneBuilder : IDisposable
{
    private readonly Scene _scene;
    private IInspectable? _inspecting;
    private IRenderable? _inspectingRenderable;
    private ISceneEditable? _inspectingEditable;
    private SceneObject? _inspectingObject;

    private int viewStencil;
    private readonly uint rectOutlineCol;

    private Vector2 _scenePosition = ImGui.GetCursorStartPos();
    private float _sceneSize = 1f;
    private Vector2 lastScenePos = Vector2.Zero;
    private Vector2 lastMovePos = Vector2.Zero;

    public SceneBuilder()
    {
        _scene = new Scene();
        _inspecting = _scene;

        Camera firstCamera = new(_scene);

        _scene.AddObject(firstCamera);
        _scene.SetActiveCamera(firstCamera);
        rectOutlineCol = ImGui.GetColorU32(ImGuiCol.Border);
    }

    public void Draw()
    {
        RenderHierarchyWindow();
        RenderInspectorWindow();

        if (_scene.ActiveCamera == null) return;

        ImGuiIOPtr io = ImGui.GetIO();
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
        {
            lastScenePos = _scenePosition;
            lastMovePos = ImGui.GetMousePos();
        }

        if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow) && ImGui.IsMouseDown(ImGuiMouseButton.Middle))
        {
            _scenePosition = lastScenePos - (lastMovePos - ImGui.GetMousePos());
        }

        _scene.Render();
        ImDrawListPtr dl = ImGui.GetBackgroundDrawList();
        Vector2 padding = ImGui.GetCursorStartPos() + ImGui.GetStyle().FramePadding;

        GuiTexture guiTexture = viewStencil switch
        {
            0 => _scene.ActiveCamera.ColorPass.RenderTexture,
            1 => _scene.ActiveCamera.LightingPass.RenderTexture,
            2 => _scene.ActiveCamera.RemovalPass.RenderTexture,
            _ => GuiManager.MissingTex
        };

        GuiTexture texture = guiTexture;
		if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow) && io.MouseWheel != 0f)
		{
			float lastSize = _sceneSize;
			_sceneSize += io.MouseWheel * 0.05f;
			_sceneSize = float.Max(_sceneSize, 0f);
			if (lastSize != _sceneSize)
			{
				_scenePosition += texture.Size / 2f * lastSize - texture.Size / 2f * _sceneSize;
			}
		}

		dl.AddImage(texture.Index, _scenePosition, _scenePosition + texture.Size * _sceneSize);
		dl.AddRect(_scenePosition, _scenePosition + texture.Size * _sceneSize, rectOutlineCol);

		if (_inspectingEditable != null && _inspectingObject != null)
		{
			ImDrawListPtr windowDrawList = ImGui.GetBackgroundDrawList();
			Vector2 scenePosition = _scenePosition;
			scenePosition.X = _scenePosition.X + (_inspectingObject.Position.X + _inspectingObject.Size.X / 2f);
			scenePosition.Y = _scenePosition.Y + (_inspectingObject.Position.Y + _inspectingObject.Size.Y / 2f);
			_inspectingEditable.RenderSceneRepresentation(windowDrawList, scenePosition);
		}

		guiTexture = viewStencil switch
		{
			0 => _scene.ActiveCamera.LightingPass.RenderTexture,
			1 => _scene.ActiveCamera.RemovalPass.RenderTexture,
			2 => _scene.ActiveCamera.ColorPass.RenderTexture,
			_ => GuiManager.MissingTex
		};

		Vector2 viewMenuSize = Vector2.Normalize(guiTexture.Size) * 32f;
		dl.AddImage(guiTexture.Index, padding, padding + viewMenuSize);
		dl.AddRect(padding, padding + Vector2.Normalize(guiTexture.Size) * 32f, rectOutlineCol);
		RectangleF mouseRect = new(padding.X, padding.Y, viewMenuSize.X, viewMenuSize.Y);
		if (!ImGui.IsAnyItemFocused() && mouseRect.Contains(ImGui.GetMousePos()))
		{
			if (ImGui.BeginTooltip())
			{
				ImGui.Text("Click to switch between views.");
				ImGui.EndTooltip();
			}

			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) viewStencil = (viewStencil + 1) % 3;
		}

        ImGui.End();
    }

    private void RenderHierarchyWindow()
    {
        if (!ImGui.Begin("Hierarchy")) return;

        if (Utils.SelectableTreeNode("Scene")) SetActiveInspect(_scene);

        foreach (SceneObject sceneObject in _scene.Objects)
        {
            sceneObject.Update();
            if (Utils.SelectableTreeNode(sceneObject.NameInScene)) SetActiveInspect(sceneObject);
        }

        ImGui.End();
    }

    private void RenderInspectorWindow()
    {
        if (!ImGui.Begin("Inspector")) return;

        if (_inspecting != null)
        {
            ImGui.SeparatorText((_inspectingObject == null) ? "Scene" : _inspectingObject.NameInScene);
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
                if (_inspectingObject is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _inspectingObject = null;
            }
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

    public void Dispose()
    {
	    _scene.Dispose();
    }
}