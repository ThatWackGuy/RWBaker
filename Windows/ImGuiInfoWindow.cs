using ImGuiNET;

namespace RWBaker.Windows;

public class ImGuiInfoWindow : Window
{
    public ImGuiInfoWindow() : base("ImGui", "_ImGuiInfo")
    {
    }

    protected override void Draw()
    {
        ImGui.ShowAboutWindow(ref open);
    }

    protected override void Destroy()
    {

    }
}