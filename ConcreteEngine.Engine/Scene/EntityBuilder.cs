using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Scene;

internal delegate void EntityCreationDel(EntityId entity, in EntityBuilder builder);

public ref struct EntityBuilder
{
    public CoreComponentBundle Data;
    
    private bool _hasSource, _hasTransform, _hasBox;

    public void SetSource(SourceComponent source)
    {
        Data.Source = source;
        _hasSource = true;
    } 
    
    public void SetTransform(in Transform transform)
    {
        Data.Transform = transform;
        _hasTransform = true;
    } 
    
    public void SetBox(in BoundingBox box)
    {
        Data.Box = box;
        _hasBox = true;
    }
    
    public void SetBox(in BoxComponent box)
    {
        Data.Box = box;
        _hasBox = true;
    }

    public readonly bool IsValid() => _hasSource && _hasTransform && _hasBox;

}