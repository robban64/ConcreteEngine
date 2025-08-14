using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Rendering.Materials;

public class MaterialStore
{
    private int _materialIdx = 0;
    private readonly Material[] _materials = new Material[RenderConsts.MaxMaterials];

    public int AddMaterial(MaterialDescription description)
    {
        if(_materialIdx == _materials.Length - 1)
            throw new IndexOutOfRangeException($"Material Store is full with size: {_materials.Length}");
        
        _materials[_materialIdx] = new Material(
            id: MaterialId.Of(_materialIdx),
            texture: description.Texture,
            shader: description.Shader,
            blend: description.Blend
        );
        
        return _materialIdx++;
    }

    public void ClearStore()
    {
        _materialIdx = 0;
        for (int i = 0; i < _materials.Length; i++)
            _materials[i] = null!;
    }

    public Material this[MaterialId key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _materials[key.Id];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Material GetMaterial(MaterialId materialId) => _materials[materialId.Id];
}