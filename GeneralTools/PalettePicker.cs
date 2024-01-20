using System.IO;
using System.Numerics;
using ImGuiNET;
using RWBaker.GraphicsTools;
using SixLabors.ImageSharp.Formats.Png;

namespace RWBaker.GeneralTools;

public class PalettePicker : Window
{
    private Vector2 pickersSize;
    private string cPaletteOutputName;

    public PalettePicker() : base("Palette Picker", "_PalettePicker")
    {
        pickersSize = new Vector2(512, 0);
        cPaletteOutputName = "";
    }

    protected override void Draw()
    {
        Begin();

        pickersSize.X = ImGui.GetWindowSize().X * 2f / 3f;
        Vector2 avail = ImGui.GetContentRegionAvail();
        Vector2 palSize = avail with { Y = avail.X / 2 } / 4;

        if (PaletteManager.Palettes.Count == 0)
        {
            ImGui.Text("Couldn't find any palettes!");
            ImGui.End();
            return;
        }

        ImGui.TextDisabled($"Detected Palettes: {PaletteManager.Palettes.Count}");

        ImGui.BeginChild("Palette Picker", pickersSize, ImGuiChildFlags.Border);
        ImGui.SeparatorText("PALETTE A");
        
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

        ImGui.Image(PaletteManager.PaletteA.DisplayTex.Index, palSize);
        
        ImGui.SeparatorText("PALETTE B");
        
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

        ImGui.Image(PaletteManager.PaletteB.DisplayTex.Index, palSize);

        ImGui.SeparatorText("PALETTE MIXED");

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

        ImGui.Image(PaletteManager.CurrentPalette.DisplayTex.Index, palSize);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("Effect Picker", Vector2.Zero, ImGuiChildFlags.Border);

        avail = ImGui.GetContentRegionAvail();
        Vector2 effectColorsSize = avail with { Y = PaletteManager.EffectColors.Size.Y * (avail.X / PaletteManager.EffectColors.Size.X) };
        Vector2 effectSize = avail with { Y = avail.X * 2 } / 8;

        GuiTexture ec = PaletteManager.EffectColors;
        ImGui.Image(ec.Index, effectColorsSize);

        ImGui.SeparatorText("EFFECT COLOR A");

        if (ImGui.SliderInt("A Index", ref context.EffectColorA, 0, (int)ec.Size.X / 2 - 1))
        {
            PaletteManager.CurrentChanged = true;
        }

        ImGui.Image(ec.Index, effectSize, new Vector2(context.EffectColorA * 2, 0) / ec.Size, new Vector2(context.EffectColorA * 2 + 2, 4) / ec.Size);

        ImGui.SeparatorText("EFFECT COLOR B");

        if (ImGui.SliderInt("B Index", ref context.EffectColorB, 0, (int)ec.Size.X / 2 - 1))
        {
            PaletteManager.CurrentChanged = true;
        }

        ImGui.Image(ec.Index, effectSize, new Vector2(context.EffectColorB * 2, 0) / ec.Size, new Vector2(context.EffectColorB * 2 + 2, 4) / ec.Size);

        ImGui.EndChild();

        ImGui.End();
    }

    protected override void Destroy()
    {
        
    }
}