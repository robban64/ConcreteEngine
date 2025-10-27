using System.Diagnostics;
using ConcreteEngine.Core.Scene.Entities;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Core.Scene;

public interface IMaterialEntityRegistry
{
    void AttachEntity(EntityId entityId, params MaterialId[] materialIds);
}
public sealed class MaterialEntityRegistry : IMaterialEntityRegistry
{
    //private List<MaterialId>[] _materials = new List<MaterialId>[32];
    private readonly Dictionary<EntityId, List<MaterialId>> _register = new(64);
    
    public void AttachEntity(EntityId entityId, params MaterialId[] materialIds)
    {
        ArgumentNullException.ThrowIfNull(materialIds);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(materialIds.Length, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(entityId.Id, 0);
        
        if (!_register.TryAdd(entityId, materialIds.ToList()))
            throw new InvalidOperationException($"Entity {entityId} already attached to material");
    }

    public List<MaterialId> GetMaterialIds(EntityId entityId)
    {
        Debug.Assert(entityId.Id > 0);
        Debug.Assert(_register.ContainsKey(entityId));
        return _register[entityId];
    }
    
}