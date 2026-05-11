using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

public struct DebugBoundsComponent(Color32 color) : IRenderComponent<DebugBoundsComponent>
{
    public static readonly Color32[] DefaultColors =
    [
        (Color32)Color4.Green, 
        (Color32)Color4.CornflowerBlue,
        (Color32)Color4.Magenta, 
        (Color32)Color4.Cyan,
        (Color32)Color4.Orange
    ];

    public Color32 Color = color;
}