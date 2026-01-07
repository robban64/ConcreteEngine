using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Scene;

internal sealed class SceneObjectProxy(SceneObject target, SceneStore store)
{
    private readonly SceneObject _target = target;
    private readonly SceneStore _store = store;
    
    public string Name => _target.Name;
    public Transform Transform => _target.Transform;

    public void SetTransform(ref Transform transform)
    {
        _target.Transform = transform;
    }
}