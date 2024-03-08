using System.Linq;
using System.Numerics;

namespace RWBaker.Rendering;

public abstract class SceneObject
{
    public readonly Scene Scene;
    public string OriginalName;
    public string NameInScene;
    public Vector3 Position;

    public SceneObject(Scene owner, string name)
    {
        Scene = owner;
        OriginalName = name;
        int repeat = owner.Objects.Count(t => t.OriginalName == name);
        NameInScene = repeat > 0 ? name + $" {repeat}" : name;
    }
}