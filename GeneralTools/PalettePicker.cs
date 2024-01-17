using System.IO;
using ImGuiNET;
using SixLabors.ImageSharp.Formats.Png;

namespace RWBaker.GeneralTools;

public class PalettePicker : Window
{
    private int paletteColorSize;
    private string cPaletteOutputName;

    public PalettePicker() : base("Palette Picker", "palette_mix")
    {
        paletteColorSize = 15;
        cPaletteOutputName = "";
    }

    protected override void Draw()
    {
        Begin();
        
        ImGui.TextDisabled($"Detected Palettes: {PaletteManager.Palettes.Count}");

        ImGui.InputTextWithHint("##input_palettes", "Palette Directory", ref context.SavedPaletteDir, 280);

        if (ImGui.Button("Get palettes"))
        {
            PaletteManager.Unload();
            PaletteManager.Load(context);
        }

        if (PaletteManager.Palettes.Count <= 0)
        {
            ImGui.Text("Couldn't find any palettes!");
            ImGui.End();
            return;
        }
        
        ImGui.Separator();
        ImGui.SliderInt("Palette Color Display Size", ref paletteColorSize, 0, 120);
        ImGui.Separator();
        
        ImGui.Text("PALETTE A");

        if (ImGui.BeginCombo("##palA_select", context.UsingPalette1))
        {
            foreach (Palette palette in PaletteManager.Palettes)
            {
                if (ImGui.Selectable(palette.Name, PaletteManager.PaletteA.Name == palette.Name))
                {
                    context.UsingPalette1 = palette.Name;
                    PaletteManager.SetA(palette);
                }

                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.Image(palette.DisplayTex.Index, palette.DisplayTex.Size * 5);
                ImGui.EndTooltip();
            }
            
            ImGui.EndCombo();
        }
        
        context.Palette1Percentage = 100 - context.Palette2Percentage;
        ImGui.SliderInt("Palette A Percentage", ref context.Palette1Percentage, 0, 100);

        ImGui.Image(PaletteManager.PaletteA.DisplayTex.Index, PaletteManager.PaletteA.DisplayTex.Size * paletteColorSize);
        
        ImGui.Separator();
        
        ImGui.Text("PALETTE B");
        
        if (ImGui.BeginCombo("##palB_select", context.UsingPalette2))
        {
            foreach (Palette palette in PaletteManager.Palettes)
            {
                if (ImGui.Selectable(palette.Name, PaletteManager.PaletteB.Name == palette.Name))
                {
                    context.UsingPalette2 = palette.Name;
                    PaletteManager.SetB(palette);
                }
                
                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.Image(palette.DisplayTex.Index, palette.DisplayTex.Size * 5);
                ImGui.EndTooltip();
            }

            ImGui.EndCombo();
        }

        context.Palette2Percentage = 100 - context.Palette1Percentage;
        ImGui.SliderInt("Palette B Percentage", ref context.Palette2Percentage, 0, 100);

        ImGui.Image(PaletteManager.PaletteB.DisplayTex.Index, PaletteManager.PaletteB.DisplayTex.Size * paletteColorSize);
        
        ImGui.Separator();
        
        ImGui.Text("MIXED PALETTE");

        if (ImGui.Button("MIX"))
        {
            PaletteManager.MixIntoCurrent(PaletteManager.PaletteA, PaletteManager.PaletteB, context.Palette1Percentage, context.Palette2Percentage);
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("SAVE"))
        {
            FileStream stream = File.Create($"{context.SavedPaletteDir}/{cPaletteOutputName}.png");
            PaletteManager.CurrentPalette.Image.Save(stream, new PngEncoder());
            stream.Close();
        }
        
        ImGui.SameLine();
        
        ImGui.InputText("##palette_name", ref cPaletteOutputName, 24);
        
        ImGui.SameLine();
        
        Utils.InfoMarker("Will be outputted to palette folder\nIf image already exists it will be overriden");

        ImGui.Image(PaletteManager.CurrentPalette.DisplayTex.Index, PaletteManager.CurrentPalette.DisplayTex.Size * paletteColorSize);

        ImGui.End();
    }

    protected override void Destroy()
    {
        
    }
}