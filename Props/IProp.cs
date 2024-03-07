using System;
using System.Numerics;
using RWBaker.RWObjects;
using Veldrid;

namespace RWBaker.Props;

public enum PropType
{
    Standard,
    VariedStandard,
    Soft,
    VariedSoft,
    SimpleDecal,
    VariedDecal,
    Antimatter
}

[Flags]
public enum PropTag
{
    None,
    RandomRotat = 2,
    RandomFlipX = 4,
    RandomFlipY = 8,
    SnapToGrid = 16,
    PostEffects = 32,
    CircularSign = 64,
    Colored = 128,
    CustomColorRainbow = 256,
    CustomColor = 512
}

public interface IProp
{
    public delegate DeviceBuffer UniformConstructor(PropObject propObject);
    public delegate Vector2 TexPosCalculator(int variation, int layer);

    public Vector3 CategoryColor();

    public string Name();
    public string ProperName();
    public string SearchName();

    public bool HasWarnings();
    public string Warnings();

    public UniformConstructor GetUniform();
    public TexPosCalculator GetTexPos();

    public ShaderSetDescription ShaderSetDescription();

    public Vector2Int Size();

    public int[] RepeatLayers();

    public int Variants();

    public void LogWarning(string warn);
}