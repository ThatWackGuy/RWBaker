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
            Palette.Palettes.Clear();
            Palette.GetPalettes();
        }

        if (Palette.Palettes.Count <= 0)
        {
            ImGui.End();
            return;
        }
        
        ImGui.Separator();
        
        ImGui.TextDisabled($"Detected Palettes: {Palette.Palettes.Count}");
        ImGui.Spacing();
        ImGui.SliderInt("Palette Color Display Size", ref paletteColorSize, 0, 120);

        if (ImGui.BeginCombo("Palette A", context.UsingPalette1))
        {
            foreach (Palette palette in Palette.Palettes)
            {
                if (ImGui.Selectable(palette.Name, Palette.A.Name == palette.Name))
                {
                    context.UsingPalette1 = palette.Name;
                    Palette.A = palette;
                }

                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.Image(palette.DisplayTex.Handle, palette.DisplayTex.Size * 5);
                ImGui.EndTooltip();
            }
            
            ImGui.EndCombo();
        }
        
        context.Palette1Percentage = 100 - context.Palette2Percentage;
        ImGui.SliderInt("Palette A Percentage", ref context.Palette1Percentage, 0, 100);

        ImGui.Image(Palette.A.DisplayTex.Handle, Palette.A.DisplayTex.Size * paletteColorSize);
        
        ImGui.Separator();
        
        if (ImGui.BeginCombo("Palette B", context.UsingPalette2))
        {
            foreach (Palette palette in Palette.Palettes)
            {
                if (ImGui.Selectable(palette.Name, Palette.B.Name == palette.Name))
                {
                    context.UsingPalette2 = palette.Name;
                    Palette.B = palette;
                }
                
                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.Image(palette.DisplayTex.Handle, palette.DisplayTex.Size * 5);
                ImGui.EndTooltip();
            }

            ImGui.EndCombo();
        }

        context.Palette2Percentage = 100 - context.Palette1Percentage;
        ImGui.SliderInt("Palette B Percentage", ref context.Palette2Percentage, 0, 100);

        ImGui.Image(Palette.B.DisplayTex.Handle, Palette.B.DisplayTex.Size * paletteColorSize);
        
        ImGui.Separator();

        if (ImGui.Button("MIX"))
        {
            Palette.Current = Palette.MixPalettes(Palette.A, Palette.B, context.Palette1Percentage, context.Palette2Percentage);
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("SAVE"))
        {
            FileStream stream = File.Create($"{context.SavedPaletteDir}/{Palette.Current.Name}.png");
            Palette.Current.ColorsImage.Save(stream, new PngEncoder());
            stream.Close();
        }
        
        ImGui.SameLine();
        
        ImGui.InputText("##palette_name", ref Palette.Current.Name, 24);
        
        ImGui.SameLine();
        
        Utils.InfoMarker("Will be outputted to palette path\nIf image already exists it will be overriden");

        ImGui.Image(Palette.Current.DisplayTex.Handle, Palette.Current.DisplayTex.Size * paletteColorSize);

        ImGui.End();
    }
}