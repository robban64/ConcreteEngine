using System.Runtime.InteropServices;
using ConcreteEngine.Engine.ECS.Definitions;
using ConcreteEngine.Engine.Worlds.Data;

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

