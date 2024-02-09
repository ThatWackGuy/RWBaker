using System;
using ImGuiNET;
using RWBaker.Gui;

namespace RWBaker.Windows;

public abstract class Window : IDisposable
{
    public string DisplayName { get; private set; }
    public readonly string InternalIdentifier;

    protected bool open;

    protected Window(string displayName, string internalIdentifier)
    {
        DisplayName = displayName;
        InternalIdentifier = internalIdentifier;
        open = true;
    }

    protected abstract void Draw();

    public virtual void Update()
    {
        if (!open)
        {
            GuiManager.RemoveWindow(this);
            return;
        }

        Draw();
    }

    protected abstract void Destroy();

    protected void Begin()
    {
        ImGui.Begin(DisplayName, ref open);
    }

    protected void Begin(ImGuiWindowFlags flags)
    {
        ImGui.Begin(DisplayName, ref open, flags);
    }

    protected void BeginNoClose()
    {
        ImGui.Begin(DisplayName);
    }

    protected void BeginNoClose(ImGuiWindowFlags flags)
    {
        ImGui.Begin(DisplayName, flags);
    }


    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this);
    }
}