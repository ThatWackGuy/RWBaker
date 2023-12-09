using System;
using System.Numerics;
using ImGuiNET;

namespace RWBaker.GeneralTools;

public class InfoWindow : Window
{
    public InfoWindow() : base("RWBaker", "_RwBakerInfo")
    {
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

    protected override void Destroy()
    {
        
    }
}

public class ImGuiInfoWindow : Window
{
    public ImGuiInfoWindow() : base("ImGui", "_ImGuiInfo")
    {
    }

    protected override void Draw()
    {
        ImGui.ShowAboutWindow(ref Open);
    }

    protected override void Destroy()
    {
        
    }
}

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

public class ExceptionWindow : Window
{
    private readonly string _exception;
    
    public ExceptionWindow(Exception exception) : base("AN EXCEPTION WAS THROWN", exception.Message)
    {
        _exception = exception.ToString();
    }

    protected override void Draw()
    {
        Begin(ImGuiWindowFlags.AlwaysAutoResize);
        
        ImGui.Text(_exception);

        if (ImGui.Button("Copy to Clipboard"))
        {
            ImGui.SetClipboardText(_exception);
        }
        
        ImGui.End();
    }

    protected override void Destroy()
    {
    }
}