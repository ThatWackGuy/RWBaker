using System.Numerics;
using ImGuiNET;
using RWBaker.Palettes;

namespace RWBaker.Windows;

public class RWObjectFetcher : Window
{
    private RWObjectManager Objects => Program.ObjectManager;
    private PaletteManager Palettes => Program.PaletteManager;

    private string _tileDir;
    private string _propDir;
    private string _palDir;

    private Vector2 _childSize;

    public RWObjectFetcher() : base("Get Graphics", "_graphicsFetcher")
    {
        _tileDir = Objects.GraphicsDir;
        _propDir = Objects.PropsDir;
        _palDir = Palettes.PaletteDir;

        _childSize = new Vector2(0, 80);
    }

    protected override void Draw()
    {
        Begin();

        ImGui.SeparatorText("Tiles");

        if (ImGui.BeginChild("TileFetcher", _childSize, ImGuiChildFlags.Border))
        {
            ImGui.InputTextWithHint("Graphics Path", "Graphics Folder Location", ref _tileDir, 540);

            if (ImGui.Button("Get Tiles"))
            {
                Objects.GetTiles(_tileDir);
            }

            if (Objects.TileLoadLogs.Length > 0) Utils.WarningMarker(Objects.TileLoadLogs);

            ImGui.EndChild();
        }

        ImGui.SeparatorText("Props");

        if(ImGui.BeginChild("PropFetcher", _childSize, ImGuiChildFlags.Border))
        {
            ImGui.InputTextWithHint("Props Path", "Props Folder Location", ref _propDir, 540);

            if (ImGui.Button("Get Props"))
            {
                Objects.GetProps(_propDir);
            }

            if (Objects.PropLoadLogs.Length > 0) Utils.WarningMarker(Objects.PropLoadLogs);

            ImGui.EndChild();
        }

        ImGui.SeparatorText("Palettes");

        if(ImGui.BeginChild("PaletteFetcher", _childSize, ImGuiChildFlags.Border))
        {
            ImGui.InputTextWithHint("Palettes Path", "Palette Folder Location", ref _palDir, 280);

            if (ImGui.Button("Get palettes"))
            {
                Palettes.GetPalettes(_palDir);
            }

            ImGui.EndChild();
        }

        ImGui.End();
    }

    protected override void Destroy()
    {

    }
}