using System.Numerics;
using ImGuiNET;

namespace RWBaker.Rendering;

/// <summary> Represents a <see cref="SceneObject"/> that has scene UI and interactions <see cref="Scene"/> </summary>
public interface ISceneEditable
{
    public void RenderSceneRepresentation(ImDrawListPtr dl, Vector2 objectScreenPos);
}