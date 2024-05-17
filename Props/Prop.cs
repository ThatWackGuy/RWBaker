using System;
using System.Numerics;
using RWBaker.Rendering;

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
    CustomColor = 512,
    EffectColorA = 1024,
    EffectColorB = 2048,
    NotTrashProp = 4076
}

public abstract class Prop
{
    public string Name { get; protected set; }
    public string ProperName { get; protected set; }
    public string SearchName { get; protected set; }

    public string Category { get; protected set; }
    public Vector3 CategoryColor { get; protected set; }

    public bool HasWarnings { get; protected set; }
    public string Warnings { get; protected set; }

    protected Prop()
    {
        HasWarnings = false;
        Warnings = "";
    }

    public abstract PropObject AsObject(Scene scene, RWObjectManager objectManager);

    public void LogWarning(string warn)
    {
        HasWarnings = true;
        Warnings += "\t" + warn  + "\n";
    }
}