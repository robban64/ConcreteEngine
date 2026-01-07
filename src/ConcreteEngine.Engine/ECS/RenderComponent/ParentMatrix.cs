using System.Numerics;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

public struct ParentMatrix: IRenderComponent<ParentMatrix>
{
    public Matrix4x4 World;

    public static implicit operator ParentMatrix(in Matrix4x4 m) => new() { World = m };
}