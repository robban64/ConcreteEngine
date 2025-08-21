#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Core.Resources;


public readonly record struct MaterialId(int Id)
{
    public static MaterialId Of(int id) => new(id);
}

public sealed class MaterialStore
{
    private int _idx = 1;

    private readonly Dictionary<MaterialId, Material> _materials;
    private readonly Dictionary<string, MaterialTemplate> _templates;
    
    public Dictionary<MaterialId, Material>.ValueCollection Materials => _materials.Values;
    
    public bool HasViewProjection { get; private set; }

    internal MaterialStore(List<MaterialTemplate> templates)
    {
        _materials = new Dictionary<MaterialId, Material>(templates.Count);
        _templates = new Dictionary<string, MaterialTemplate>(templates.Count);
        
        foreach (var template in templates)
            _templates.Add(template.Name, template);

        foreach (var template in templates)
            CreateMaterialFromTemplate(template.Name);

    }

    public Material CreateMaterialFromTemplate(string name)
    {
        if(!_templates.TryGetValue(name, out var template))
            throw new KeyNotFoundException($"Material Template with name {name} was not found");

        var id = new MaterialId(_idx++);
        var mat = new Material(id, template);
        _materials.Add(id, mat);
        return mat;
    }
    


    public Material this[MaterialId key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _materials[key];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material GetMaterial(MaterialId materialId) => _materials[materialId];
}