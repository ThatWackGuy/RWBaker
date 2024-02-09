using System;
using ImGuiNET;

namespace RWBaker.Windows;

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