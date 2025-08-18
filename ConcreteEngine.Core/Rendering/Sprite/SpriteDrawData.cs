#region

using System.Numerics;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering.Sprite;

public readonly record struct SpriteDrawData(
    Vector2 Position,
    Vector2 Scale,
    UvRect Uv);