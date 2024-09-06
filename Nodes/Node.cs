using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using RWBaker.Windows;

namespace RWBaker.Nodes;

public class NodeInput
{
    private GraphicsNode _owner;
    private uint _color;
    private NodeOutput? _connected = null;
    private bool _hidden = false;
    private int _typeHash;

    public readonly string Name;

    public NodeInput(GraphicsNode owner, string name, uint color, Type accepting)
    {
        if (name.Contains(' ') || name.Contains('\n')) throw new ArgumentException("IO names must be short");

        _owner = owner;
        _color = color;
        _typeHash = accepting.GetHashCode();
        Name = name;
    }

    public bool TryGetValue<T>([MaybeNullWhen(false)] out T val)
    {
        val = _connected == null ? default : _connected.Get<T>();
        return val != null;
    }

    public int AcceptingType() => _typeHash;

    public void Render(ImDrawListPtr dl, ref Vector2 pos, float scale)
    {
        if (_hidden) return;

        if (_connected != null) dl.AddRectFilled(pos + new Vector2(2.5f) * scale, pos + new Vector2(10) * scale, _color);
        dl.AddRect(pos, pos + new Vector2(12) * scale, _color, 0, ImDrawFlags.None, 2);
        dl.AddText(pos + new Vector2(20, -1f), ImGui.GetColorU32(ImGuiCol.Text), Name);

        pos.Y += 30;
    }
}

public class NodeOutput
{
    private GraphicsNode _owner;

    private uint _color;
    private NodeInput? _connected;
    private bool _hidden;
    private object _val;
    private int _typeHash;

    public readonly string Name;

    public NodeOutput(GraphicsNode owner, string name, uint color, Type outputting)
    {
        if (name.Contains(' ') || name.Contains('\n')) throw new ArgumentException("IO names must be short");

        _owner = owner;
        _color = color;
        _typeHash = outputting.GetHashCode();
        Name = name;
    }

    public T Get<T>() => (T)_val;

    public void Set<T>(T val)
    {
        if (typeof(T).GetHashCode() != _typeHash) throw new ArgumentException("The output set should be the same type as output type");
        _val = val ?? throw new ArgumentNullException(nameof(val));
    }

    public void Render(ImDrawListPtr dl, ref Vector2 pos, float scale)
    {
        if (_hidden) return;

        dl.AddCircleFilled(pos, 3 * scale, _color);
        dl.AddCircle(pos, 6 * scale, ImGui.GetColorU32(ImGuiCol.Border, 1), 20, 2);
        dl.AddText(pos + new Vector2(-(ImGui.CalcTextSize(Name).X + 11), -6.5f), ImGui.GetColorU32(ImGuiCol.Text), Name);
    }
}

public abstract class GraphicsNode
{
    private bool _middleSpace;

    protected Vector2 position;
    protected string title;
    protected uint color;
    protected List<NodeInput> inputs = [];
    protected List<NodeOutput> outputs = [];

    public readonly NodeBoard Board;

    protected GraphicsNode(NodeBoard board, string title, uint color, bool useMiddleSpace)
    {
        _middleSpace = useMiddleSpace;

        Board = board;
        position = new Vector2(30, 30);

        if (title.Length > 20) throw new ArgumentException("Keep your damn node titles short");
        this.title = title;
        this.color = color;
    }

    protected abstract void MiddleUI();

    public void Render(ImDrawListPtr dl, float scale)
    {
        Vector2 cursor = ImGui.GetCursorScreenPos();
        Vector2 pos = cursor + position;

        float maxInputX = float.Max(inputs.Select(i => ImGui.CalcTextSize(i.Name).X).DefaultIfEmpty().Max(), 50);
        float maxX = 10 + float.Max(
            ImGui.CalcTextSize(title).X,
            maxInputX
            + (_middleSpace ? 200 : 0)
            + float.Max(outputs.Select(o => ImGui.CalcTextSize(o.Name).X).DefaultIfEmpty().Max(), 50)
        );
        float maxY = 20 * int.Max(inputs.Count, outputs.Count) + 20;

        Vector2 titleSize = ImGui.CalcTextSize(title);
        Vector2 topSize = (pos + new Vector2(maxX, 10 + titleSize.Y)) * scale;
        Vector2 topEnd = pos + new Vector2(0, 10 + titleSize.Y);

        float round = ImGui.GetStyle().TabRounding;
        uint borderC = ImGui.GetColorU32(ImGuiCol.Border);
        uint nodeGuiBg = ImGui.GetColorU32(ImGuiCol.PopupBg);

        // Top bar
        /*
         * /----------------------------\
         * | TITLE                      |
         * |----------------------------|
         */
        dl.AddRectFilled(pos, topSize, color, round, ImDrawFlags.RoundCornersTop);
        dl.AddText(pos + new Vector2(5, 5), ImGui.GetColorU32(ImGuiCol.Text), title);

        // Actual UI
        /*
         *       |--------------------------------|
         * ------+-[] IN |               | OUT () |
         *       | [] IN |               | OUT () |
         *       | [] IN |               | OUT ()-+-------------
         *       | [] IN |               | OUT () |
         *       \--------------------------------/
         */
        dl.AddRectFilled(topEnd * scale, (topEnd + new Vector2(maxX, maxY)) * scale, nodeGuiBg, round, ImDrawFlags.RoundCornersBottom);

        // borders
        dl.AddLine(topEnd * scale, (topEnd + new Vector2(maxX, 0)) * scale, borderC);
        dl.AddRect(pos, (topEnd + new Vector2(maxX, maxY)) * scale, borderC, round, ImDrawFlags.RoundCornersAll);

        topEnd.Y += 10;
        Vector2 ioPos = topEnd + new Vector2(10, 0);
        foreach (NodeInput input in inputs)
        {
            input.Render(dl, ref ioPos, scale);
        }

        ImGui.SetCursorScreenPos(topEnd + new Vector2(maxInputX + 30, 0));
        if (ImGui.BeginChild("middle", new Vector2(200, maxY)))
        {
            MiddleUI();

            ImGui.EndChild();
        }

        ioPos = topEnd + new Vector2(maxX - 13, 6.5f);
        foreach (NodeOutput output in outputs)
        {
            output.Render(dl, ref ioPos, scale);
        }
    }
}

public class ConstantInt : GraphicsNode
{
    public ConstantInt(NodeBoard board) : base(board, "Number", ImGui.GetColorU32(new Vector4(0, 0.9f, 0.3f, 1)), true)
    {
        var outInt = new NodeOutput(this, "output", color, typeof(int)); outInt.Set(0);

        outputs.Add(outInt);
    }

    protected override void MiddleUI()
    {
        int outp = outputs[0].Get<int>();
        if (ImGui.SliderInt("##output_int", ref outp, 0, 100)) outputs[0].Set(outp);
    }
}

public class ConstantAdder : GraphicsNode
{
    public ConstantAdder(NodeBoard board) : base(board, "Add", ImGui.GetColorU32(new Vector4(0.7f, 0.2f, 0.4f, 1)), false)
    {
        inputs.Add(new NodeInput(this, "A", color, typeof(int)));
        inputs.Add(new NodeInput(this, "B", color, typeof(int)));

        var outInt = new NodeOutput(this, "A+B", color, typeof(int)); outInt.Set(0);
        outputs.Add(outInt);
    }

    protected override void MiddleUI()
    {

    }
}

public class NodeBoard(string name) : Window(name, $"_nodeGraph_{name}")
{
    private static readonly List<Type> RegisteredNodes = [];
    private float _size = 1f;

    public string GraphName = name;
    public List<GraphicsNode> Nodes = [];

    protected override void Draw()
    {
        if (!Begin(ImGuiWindowFlags.MenuBar)) return;
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Load"))
                {

                }

                if (ImGui.MenuItem("Save"))
                {

                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Nodes"))
            {
                foreach (Type node in RegisteredNodes.Where(node => ImGui.MenuItem(node.Name)))
                {
                    Nodes.Add((GraphicsNode)Activator.CreateInstance(node, this)!);
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        ImDrawListPtr dl = ImGui.GetWindowDrawList();

        foreach (GraphicsNode node in Nodes)
        {
            node.Render(dl, _size);
        }
    }

    protected override void Destroy() { }

    public static void RegisterNode<T>() where T : GraphicsNode
    {
        RegisteredNodes.Add(typeof(T));
    }

    public string CreateBakerNodeGraph()
    {
        /*
         * NODE GRAPH SAVE SYNTAX: TOKEN_NAME[SIZE_IN_BYTES]
         * NAME_SIZE[8] NAME...
         * NODE_COUNT[8]
         * NODE_POS[8] NODE_TYPE_HASH[8] ... STRIDE : 16
         * NODE_N_OUTPUT[8] NODE_N_INPUT[8]... STRIDE : 16
         */

        throw new NotImplementedException();
    }
}