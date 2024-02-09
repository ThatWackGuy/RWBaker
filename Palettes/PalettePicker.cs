using System.IO;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Windows;
using SixLabors.ImageSharp.Formats.Png;

namespace RWBaker.Palettes;

public class PalettePicker : Window
{
    private PaletteManager palettes => Program.PaletteManager;
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

        if (palettes.Palettes.Count == 0)
        {
            ImGui.Text("Couldn't find any palettes!");
            ImGui.End();
            return;
        }

        ImGui.TextDisabled($"Detected Palettes: {palettes.Palettes.Count}");

        ImGui.BeginChild("Palette Picker", pickersSize, ImGuiChildFlags.Border);
        ImGui.SeparatorText("PALETTE A");

        if (ImGui.BeginCombo("##palA_select", palettes.PaletteA.Name))
        {
            foreach (Palette palette in palettes.Palettes)
            {
                if (ImGui.Selectable(palette.Name, palettes.PaletteA.Name == palette.Name))
                {
                    palettes.PaletteA = palette;
                }

                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.Image(palette.DisplayTex.Index, palette.DisplayTex.Size * 5);
                ImGui.EndTooltip();
            }

            ImGui.EndCombo();
        }

        ImGui.Image(palettes.PaletteA.DisplayTex.Index, palSize);

        ImGui.SeparatorText("PALETTE B");

        if (ImGui.BeginCombo("##palB_select", palettes.PaletteB.Name))
        {
            foreach (Palette palette in palettes.Palettes)
            {
                if (ImGui.Selectable(palette.Name, palettes.PaletteB.Name == palette.Name))
                {
                    palettes.PaletteB = palette;
                }

                if (!ImGui.IsItemHovered()) continue;
                if (!ImGui.BeginTooltip()) continue;
                ImGui.Image(palette.DisplayTex.Index, palette.DisplayTex.Size * 5);
                ImGui.EndTooltip();
            }

            ImGui.EndCombo();
        }

        ImGui.Image(palettes.PaletteB.DisplayTex.Index, palSize);

        ImGui.SeparatorText("PALETTE MIXED");

        ImGui.SliderInt("Palette Blend", ref palettes.PaletteBlend, 0, 100);

        if (ImGui.Button("MIX"))
        {
            if (palettes.CurrentPalette.Name != $"{palettes.PaletteA.Name} {palettes.PaletteBlend} {palettes.PaletteB.Name}")
            {
                if (palettes.CurrentPalette.isMixed) palettes.CurrentPalette.DisplayTex.Dispose();
                palettes.CurrentPalette = Palette.MixPalettes(palettes.PaletteA, palettes.PaletteB, palettes.PaletteBlend);
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("SAVE"))
        {
            FileStream stream = File.Create(Path.Combine(palettes.PaletteDir, $"{cPaletteOutputName}.png"));
            palettes.CurrentPalette.Image.Save(stream, new PngEncoder());
            stream.Close();
        }

        ImGui.SameLine();

        ImGui.InputText("##palette_name", ref cPaletteOutputName, 24);

        ImGui.SameLine();

        Utils.InfoMarker("Will be outputted to palette folder\nIf image already exists it will be overriden");

        ImGui.Image(palettes.CurrentPalette.DisplayTex.Index, palSize);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("Effect Picker", Vector2.Zero, ImGuiChildFlags.Border);

        avail = ImGui.GetContentRegionAvail();
        Vector2 effectColorsSize = avail with { Y = palettes.EffectColors.Size.Y * (avail.X / palettes.EffectColors.Size.X) };
        Vector2 effectSize = avail with { Y = avail.X * 2 } / 8;

        GuiTexture ec = palettes.EffectColors;
        ImGui.Image(ec.Index, effectColorsSize);

        ImGui.SeparatorText("EFFECT COLOR A");

        int refEffect = palettes.EffectColorA;
        if (ImGui.SliderInt("A Index", ref refEffect, 0, (int)ec.Size.X / 2 - 1))
        {
            palettes.EffectColorA = refEffect;
        }

        ImGui.Image(ec.Index, effectSize, new Vector2(palettes.EffectColorA * 2, 0) / ec.Size, new Vector2(palettes.EffectColorA * 2 + 2, 4) / ec.Size);

        ImGui.SeparatorText("EFFECT COLOR B");

        refEffect = palettes.EffectColorB;
        if (ImGui.SliderInt("B Index", ref refEffect, 0, (int)ec.Size.X / 2 - 1))
        {
            palettes.EffectColorB = refEffect;
        }

        ImGui.Image(ec.Index, effectSize, new Vector2(palettes.EffectColorB * 2, 0) / ec.Size, new Vector2(palettes.EffectColorB * 2 + 2, 4) / ec.Size);

        ImGui.EndChild();

        ImGui.End();
    }

    protected override void Destroy()
    {

    }
}