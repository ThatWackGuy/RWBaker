using System;
using ImGuiNET;
using RWBaker.GraphicsTools;

namespace RWBaker;

public abstract class Window : IDisposable
{
    public string DisplayName;
    public readonly string InternalIdentifier;

    public bool Open;

    protected Window(string displayName, string internalIdentifier)
    {
        DisplayName = displayName;
        InternalIdentifier = internalIdentifier;
    }

    protected abstract void Draw();

    public virtual void Update()
    {
        if (!Open)
        {
            GuiManager.RemoveWindow(this);
            return;
        }

        Draw();
    }
    
    protected abstract void Destroy();

    protected void Begin()
    {
        ImGui.Begin(DisplayName, ref Open);
    }

    protected void Begin(ImGuiWindowFlags flags)
    {
        ImGui.Begin(DisplayName, ref Open, flags);
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