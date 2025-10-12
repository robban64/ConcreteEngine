#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public readonly record struct MaterialId(int Id)
{
    public static implicit operator int(MaterialId id) => id.Id;
    public static explicit operator MaterialId(int value) => new(value);
}

public sealed class MaterialStore
{
    private readonly Dictionary<string, MaterialTemplate> _templates;

    private List<Material> _materials;

    public IReadOnlyList<Material> Materials => _materials;


    internal MaterialStore(IReadOnlyList<MaterialTemplate> templates)
    {
        _materials = new List<Material>(templates.Count);
        _templates = new Dictionary<string, MaterialTemplate>(templates.Count);

        foreach (var template in templates)
            _templates.Add(template.Name, template);

        foreach (var template in templates)
            CreateMaterialFromTemplate(template.Name);
    }

    public Material CreateMaterialFromTemplate(string templateName)
    {
        if (!_templates.TryGetValue(templateName, out var template))
            throw new KeyNotFoundException($"Material Template with name {templateName} was not found");

        var id = new MaterialId(_materials.Count + 1);
        var mat = new Material(id, template);
        _materials.Add(mat);
        return mat;
    }

    public void RemoveMaterial(Material material) => _materials.Remove(material);


    public MaterialTemplate GetTemplate(string templateName) => _templates[templateName];

    public bool TryGetTemplate(string templateName, out MaterialTemplate template) =>
        _templates.TryGetValue(templateName, out template);


    public Material this[MaterialId id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _materials[id.Id - 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material GetMaterial(MaterialId id) => _materials[id.Id - 1];
}