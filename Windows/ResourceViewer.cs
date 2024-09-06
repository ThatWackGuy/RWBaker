using System;
using ImGuiNET;
using RWBaker.Gui;

namespace RWBaker.Windows;

public class ResourceViewer() : Window("Resources", "_DebugResources")
{
    protected override void Draw()
    {
        Begin();

        ImGui.TreePush("_textures");

        foreach (WeakReference<GuiTexture> texture in GuiTexture.GetAllTextures())
        {
            if (!texture.TryGetTarget(out var tex))
            {
                ImGui.Text("Texture is null");
                continue;
            }

            if (ImGui.TreeNode($"[{tex.Index}] {tex.Name}"))
            {
                ImGui.Image(tex.Index, tex.Size);

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