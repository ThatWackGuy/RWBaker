using System;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Gui;
using RWBaker.Rendering;
using Veldrid;

namespace RWBaker.Props;

public class PropObject : SceneObject, IRenderable, IInspectable, ISceneEditable, IDisposable
{
    public delegate RenderDescription CompleteDescription(Mesh mesh, Vector3 position, Matrix4x4 rotation, PropObject instance, Camera camera, Texture texture);
    public delegate Vector2 TexPosCalculator(int variation, int layer);

    public readonly string Name;
    public readonly int[] RenderRepeatLayers;
    public readonly int LayerCount;
    public float Rotation; // Rotation in radians

    private readonly CompleteDescription _completeDesc;
    private readonly TexPosCalculator _texPosCalculator;

    public readonly int Variants;
    private int _renderVariation;
    public int RenderVariation
    {
        get => _renderVariation;

        set => _renderVariation = int.Clamp(value, 0, Variants - 1);
    }

    public readonly int Depth;

    public readonly GuiTexture CachedTexture;

    public readonly Mesh Mesh;

    /// <summary>Creates an empty prop object</summary>
    /// <remarks>Only use for lack of prop data!</remarks>
    /// <exception cref="Exception">throws if used for rendering</exception>
    public PropObject() : base(null!, "")
    {
        Name = "";

        _completeDesc = (_, _, _, _, _, _) => throw new Exception();
        _texPosCalculator = (_, _) => throw new Exception();

        RenderRepeatLayers = new[] { 1 };
        LayerCount = 1;

        Size = Vector3.One;

        Variants = 1;

        Depth = 1;

        CachedTexture = GuiManager.MissingTex;

        Mesh = new Mesh();
    }

    public PropObject(string name, string properName, CompleteDescription completeDescription, TexPosCalculator texPosCalculator, Vector2 size, int[] repeatLayers, int variants, Scene scene, RWObjectManager manager) : base(scene, properName)
    {
        Name = name;

        string texturePath = Path.Combine(manager.PropsDir, $"{Name}.png");

        if (!File.Exists(texturePath))
        {
            throw new FileNotFoundException("Please check the names or if the file has been deleted!");
        }

        _completeDesc = completeDescription;
        _texPosCalculator = texPosCalculator;

        RenderRepeatLayers = repeatLayers;

        if (RenderRepeatLayers.Length > 30)
        {
            RenderRepeatLayers = RenderRepeatLayers[..30];
        }

        if (RenderRepeatLayers.Sum() >= 30)
        {
            int check = 0;
            for (int i = 0; i < RenderRepeatLayers.Length; i++)
            {
                int repeat = RenderRepeatLayers[i];

                if (check + repeat > 30)
                {
                    RenderRepeatLayers = RenderRepeatLayers[..i];
                    Array.Resize(ref RenderRepeatLayers, i + 1);
                    RenderRepeatLayers[i] = 30 - check;
                    break;
                }

                check += repeat;
            }
        }

        LayerCount = RenderRepeatLayers.Sum();
        Depth = RenderRepeatLayers.Sum();

        Variants = variants;

        Size = new Vector3(size, LayerCount);

        Position = new Vector3(-Size.X / 2,  -Size.Y / 2,0);

        if (!GuiTexture.TryGetTexture($"_prop{OriginalName}", out CachedTexture!))
        {
            CachedTexture = GuiTexture.Create($"_prop{OriginalName}", GuiManager.TextureFromImage(texturePath));
        }
        CachedTexture.Use();

        Mesh = new();
        BuildLayerMesh();
    }

    public void RenderInspector()
    {
        ImGui.DragFloat("X", ref Position.X);
        ImGui.DragFloat("Y", ref Position.Y);

        int z = (int)Position.Z;
        ImGui.SliderInt("Layer", ref z, 0, 29 - Depth);
        Position.Z = z;

        if (Variants > 1)
        {
            ImGui.SliderInt("Variant", ref _renderVariation, 0, Variants - 1);
        }

        ImGui.SliderAngle("Rotate", ref Rotation);
    }

    public void RenderSceneRepresentation(ImDrawListPtr dl, Vector2 objectScreenPos)
    {
        /*
        // Y ARROW
        Vector2 YDragEnd = objectScreenPos with { Y = objectScreenPos.Y - 70 };
        dl.AddLine(objectScreenPos, YDragEnd, Utils.IM_RED, 4);
        YDragEnd.Y -= 5;
        dl.AddTriangleFilled(YDragEnd with { X = YDragEnd.X - 6, Y = YDragEnd.Y + 12 }, YDragEnd, YDragEnd with { X = YDragEnd.X + 6, Y = YDragEnd.Y + 12 }, Utils.IM_RED);

        // X ARROW
        YDragEnd = objectScreenPos with { X = objectScreenPos.X + 70 };
        dl.AddLine(objectScreenPos, YDragEnd, Utils.IM_GREEN, 4);
        YDragEnd.X += 5;
        dl.AddTriangleFilled(YDragEnd with { X = YDragEnd.X - 12, Y = YDragEnd.Y - 6 }, YDragEnd with { X = YDragEnd.X - 12, Y = YDragEnd.Y + 6 }, YDragEnd, Utils.IM_GREEN);

        // MIDDLE
        dl.AddCircleFilled(objectScreenPos, 6, Utils.IM_WHITE, 4);
        */
    }

    public RenderDescription GetRenderDescription(Camera camera) => _completeDesc(Mesh, Position, Matrix4x4.CreateRotationZ(Rotation, Position + Size / 2),  this, camera, CachedTexture.Texture);

    private void BuildLayerMesh()
    {
        Mesh.Clear();

        Mesh.ReadyMerge(LayerCount * 4, LayerCount * 6, true);

        int renderLayer = 0;
        for (int imgLayer = 0; imgLayer < RenderRepeatLayers.Length; imgLayer++)
        {
            if (RenderRepeatLayers[imgLayer] == 0) continue;

            // box tiles calculate texture coords in-shader
            Vector2 texPos = _texPosCalculator(RenderVariation, imgLayer);

            for (int repeat = 0; repeat < RenderRepeatLayers[imgLayer]; repeat++)
            {
                Mesh.MergeQuad([
                    new Vertex(
                        new Vector3(0, Size.Y, renderLayer),
                        texPos,
                        RgbaFloat.Clear
                    ), // Top Left

                    new Vertex(
                        new Vector3(Size.X, Size.Y, renderLayer),
                        texPos with { X = texPos.X + Size.X },
                        RgbaFloat.Clear
                    ), // Top Right

                    new Vertex(
                        new Vector3(Size.X, 0, renderLayer),
                        new Vector2(texPos.X + Size.X, texPos.Y + Size.Y),
                        RgbaFloat.Clear
                    ), // Bottom Right

                    new Vertex(
                        new Vector3(0, 0, renderLayer),
                        texPos with { Y = texPos.Y + Size.Y },
                        RgbaFloat.Clear
                    ) // Bottom Left
                ]);

                renderLayer++;
            }
        }

        Mesh.AllocatedMergeOver();
    }

    public Vector2Int GetRenderSize(Camera camera) => new Vector2Int((int)Size.X, (int)Size.Y) + (LayerCount - 1) * Vector2.One; // TODO: FIX

    public void Dispose()
    {
        CachedTexture.Dispose();

        GC.SuppressFinalize(this);
    }
}