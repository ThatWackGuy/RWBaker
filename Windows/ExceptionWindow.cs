using System;
using ImGuiNET;

namespace RWBaker.Windows;

public class ExceptionWindow(Exception exception, string name, Action<ExceptionWindow>? destroyCallback) : Window(name, exception.Message)
{
    private readonly string _exception = exception.ToString();

    protected override void Draw()
    {
        if (!Begin(ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.Text(_exception);

        if (ImGui.Button("Copy to Clipboard"))
        {
            ImGui.SetClipboardText(_exception);
        }

        ImGui.End();
    }

    protected override void Destroy()
    {
        destroyCallback?.Invoke(this);
    }

    public void Close() => open = false;
}