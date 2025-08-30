using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Scene.Nodes;

public enum NodeBehaviorKind
{
    Nothing,
    Sprite,
    Tilemap,
    Particle,
}

public interface INodeBehaviour
{
    NodeBehaviorKind BehaviorKind { get; }
    bool ValidateChildNode(SceneNode child);
}

public sealed class NothingBehaviour : INodeBehaviour
{
    public NodeBehaviorKind BehaviorKind => NodeBehaviorKind.Nothing;

    public bool ValidateChildNode(SceneNode child) => true;

    public static NothingBehaviour Instance { get; } = new();
}

public sealed class SpriteBehaviour : INodeBehaviour
{
    public NodeBehaviorKind BehaviorKind => NodeBehaviorKind.Sprite;
    public MaterialId MaterialId { get; set; }
    public bool Batched { get; set; }
    public Rectangle<int> SourceRectangle { get; set; }
    public Vector2 UvScale { get; set; }
    public Vector2 PreviousPosition { get; set; }
    public UvRect GetUvRect() => UvRect.GetInsetUv(SourceRectangle, UvScale);

    public bool ValidateChildNode(SceneNode child) => true;
}

public sealed class TilemapBehaviour : INodeBehaviour
{
    public NodeBehaviorKind BehaviorKind => NodeBehaviorKind.Tilemap;

    public bool ValidateChildNode(SceneNode child) => true;
}

public sealed class LightBehaviour : INodeBehaviour
{
    public NodeBehaviorKind BehaviorKind => NodeBehaviorKind.Sprite;

    public Vector2 Position { get; set; }
    public Vector3 Color { get; set; } = new(0.7f, 0.8f, 1.0f);
    public float Radius { get; set; } = 100;
    public float Intensity { get; set; } = 1;

    public bool ValidateChildNode(SceneNode child) => true;
}