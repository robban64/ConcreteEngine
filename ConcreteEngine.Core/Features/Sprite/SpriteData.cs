#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;

public struct SpriteData
{
    Vector2D<short> Position;
    Vector2D<short> Size;
    Vector2D<byte> AtlasLocation;
}