using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct DebugBoundsComponent(ColorRgba color) : IRenderComponent<DebugBoundsComponent>
{
    public static readonly ColorRgba[] DefaultColors =
    [
        (ColorRgba)Color4.Green,
        (ColorRgba)Color4.CornflowerBlue,
        (ColorRgba)Color4.Magenta,
        (ColorRgba)Color4.Cyan,
        (ColorRgba)Color4.Orange
    ];

    public ColorRgba Color = color;
}