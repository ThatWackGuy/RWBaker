using System;
using ImGuiNET;

namespace RWBaker;

public abstract class Window : IDisposable
{
    public string DisplayName;
    public readonly string InternalIdentifier;

    public bool Open;
    public bool MarkedForDeletion;

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
            MarkedForDeletion = true;
            return;
        }

        Draw();
    }

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
        GC.SuppressFinalize(this);
    }
}