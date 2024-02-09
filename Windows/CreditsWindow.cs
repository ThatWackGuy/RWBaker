using ImGuiNET;

namespace RWBaker.Windows;

public class CreditsWindow : Window
{
    public CreditsWindow() : base("Credits", "_credits")
    {
    }

    protected override void Draw()
    {
        // TODO: ADD CREDITS
        Begin(ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.End();
    }

    protected override void Destroy()
    {

    }
}