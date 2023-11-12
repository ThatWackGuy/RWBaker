using System;
using System.Numerics;
using ImGuiNET;
using RWBaker.GraphicsTools;

namespace RWBaker.GeneralTools;

public class InfoWindow : Window
{
    public InfoWindow() : base("RWBaker", "_RwBakerInfo")
    {
        Open = true;
    }

    protected override void Draw()
    {
        Begin(ImGuiWindowFlags.AlwaysAutoResize);
        
        ImGui.Image(Program.IconTexture.Index, Program.IconTexture.Size, Vector2.Zero, Vector2.One);
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
}

public class ImGuiInfoWindow : Window
{
    public ImGuiInfoWindow() : base("ImGui", "_ImGuiInfo")
    {
        Open = true;
    }

    protected override void Draw()
    {
        ImGui.ShowAboutWindow(ref Open);
    }
}

public class CreditsWindow : Window
{
    public CreditsWindow() : base("Credits", "_credits")
    {
        Open = true;
    }

    protected override void Draw()
    {
        // TODO: ADD CREDITS
        Begin(ImGuiWindowFlags.AlwaysAutoResize);
        
        ImGui.Text("RWBaker 0.0.1");
        ImGui.TableHeader("CODE PEOPLES");
        ImGui.SameLine();
        Utils.InfoMarker("ADD PEOPLE!");
        
        ImGui.TableHeader("ART PEOPLES");
        ImGui.SameLine();
        Utils.InfoMarker("ADD PEOPLE!");

        ImGui.End();
    }
}