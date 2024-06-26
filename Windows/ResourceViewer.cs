using ImGuiNET;
using RWBaker.Gui;

namespace RWBaker.Windows;

public class ResourceViewer() : Window("Resources", "_DebugResources")
{
    protected override void Draw()
    {
        Begin();

        ImGui.TreePush("_textures");

        foreach (GuiTexture texture in GuiTexture.GetAllTextures())
        {
            if (ImGui.TreeNode($"[{texture.Index}] {texture.Name}"))
            {
                ImGui.Image(texture.Index, texture.Size);

                ImGui.TextDisabled($"Size: {texture.Size}\nRefCounter: {texture.RefCounter}");

                ImGui.TreePop();
            }
        }

        ImGui.TreePop();

        ImGui.End();
    }

    protected override void Destroy()
    {

    }
}