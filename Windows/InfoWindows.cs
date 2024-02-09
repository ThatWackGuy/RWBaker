using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;

namespace RWBaker.Windows;

public class InfoWindow : Window
{
    public InfoWindow() : base("RWBaker", "_RwBakerInfo")
    {
    }

    protected override void Draw()
    {
        Begin(ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Image(GuiManager.IconTexture.Index, GuiManager.IconTexture.Size, Vector2.Zero, Vector2.One);
        ImGui.Text("RWBaker 0.0.1");
        ImGui.Text("Made With:");

        ImGui.Text("ImGuiNET");
        ImGui.SameLine();
        Utils.InfoMarker("ADD LINK!"); // TODO: ADD LINK

        ImGui.Text("Dear ImGui");
        ImGui.SameLine();
        Utils.InfoMarker("ADD LINK!"); // TODO: ADD LINK

        ImGui.End();
    }

    protected override void Destroy()
    {

    }
}