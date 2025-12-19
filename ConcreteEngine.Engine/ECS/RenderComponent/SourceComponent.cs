using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct SourceComponent(
    ModelId model,
    MaterialTagKey materialTagKey,
    EntitySourceKind kind) : IRenderComponent<SourceComponent>
{
    public ModelId Model = model;
    public MaterialTagKey MaterialKey = materialTagKey;
    public EntitySourceKind Kind = kind;
}

