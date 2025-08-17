#region

using System.Numerics;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Rendering.Sprite;

public readonly record struct SpriteDrawData(
    Vector2 Position,
    Vector2 Scale,
    UvRect Uv);