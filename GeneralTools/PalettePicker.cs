using System.IO;
using ImGuiNET;
using SixLabors.ImageSharp.Formats.Png;

namespace RWBaker.GeneralTools;

public class PalettePicker : Window
{
    private readonly Context context;
    
    private int paletteColorSize;

    public PalettePicker() : base("Palette Picker", "palette_mix")
    {
        Open = true;

        context = Context.GetContext();
        
        paletteColorSize = 15;
    }

    protected override void Draw()
    {
        Begin();
        
        ImGui.InputTextWithHint("##input_palettes", "Palette Directory", ref context.SavedPaletteDir, 280);

        if (ImGui.Button("Get palettes"))
        {
            Program.Palettes.Clear();
            RWUtils.GetPalettes();
        }

        if (Program.Palettes.Count <= 0)
        {
            ImGui.End();
            return;
        }
        
        ImGui.Separator();
        
        ImGui.TextDisabled($"Detected Palettes: {Program.Palettes.Count}");
        ImGui.Spacing();
        ImGui.SliderInt("Palette Color Display Size", ref paletteColorSize, 0, 120);

        if (ImGui.BeginCombo("Palette A", context.UsingPalette1))
        {
            foreach (Palette palette in Program.Palettes)
            {
                if (ImGui.Selectable(palette.Name, Program.PaletteA.Name == palette.Name))
                {
                    context.UsingPalette1 = palette.Name;
                    Program.PaletteA = palette;
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

        ImGui.Image(Program.PaletteA.DisplayTex.Index, Program.PaletteA.DisplayTex.Size * paletteColorSize);
        
        ImGui.Separator();
        
        if (ImGui.BeginCombo("Palette B", context.UsingPalette2))
        {
            foreach (Palette palette in Program.Palettes)
            {
                if (ImGui.Selectable(palette.Name, Program.PaletteB.Name == palette.Name))
                {
                    context.UsingPalette2 = palette.Name;
                    Program.PaletteB = palette;
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

        ImGui.Image(Program.PaletteB.DisplayTex.Index, Program.PaletteB.DisplayTex.Size * paletteColorSize);
        
        ImGui.Separator();

        if (ImGui.Button("MIX"))
        {
            Program.CurrentPalette = Palette.MixPalettes(Program.PaletteA, Program.PaletteB, context.Palette1Percentage, context.Palette2Percentage);
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("SAVE"))
        {
            FileStream stream = File.Create($"{context.SavedPaletteDir}/{Program.CurrentPalette.Name}.png");
            Program.CurrentPalette.Image.Save(stream, new PngEncoder());
            stream.Close();
        }
        
        ImGui.SameLine();
        
        ImGui.InputText("##palette_name", ref Program.CurrentPalette.Name, 24);
        
        ImGui.SameLine();
        
        Utils.InfoMarker("Will be outputted to palette folder\nIf image already exists it will be overriden");

        ImGui.Image(Program.CurrentPalette.DisplayTex.Index, Program.CurrentPalette.DisplayTex.Size * paletteColorSize);

        ImGui.End();
    }

    protected override void Destroy()
    {
        
    }
}