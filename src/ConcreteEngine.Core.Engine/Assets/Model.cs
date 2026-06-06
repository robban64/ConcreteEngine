using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MeshEntry(string name, MeshInfo info)
{
    public readonly string Name = name;
    public MeshId MeshId;
    public MeshInfo Info = info;
    public Matrix4x4 WorldTransform;
    public BoundingBox LocalBounds;
}

public sealed class Model : AssetObject
{
    public MeshEntry[] Meshes { get; }
    public ModelAnimation? Animation { get; }

    public readonly ModelInfo Info;
    public readonly BoundingBox Bounds;

    private readonly Texture[] _textures;
    private readonly Material[] _materials;

    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;


    public Model(
        string name, 
        AssetId id, 
        Guid gid,
        in ModelInfo modelInfo,
        in BoundingBox bounds,
        MeshEntry[] meshes,
        ModelAnimation? animation) : base(name,id,gid)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meshes.Length, modelInfo.MeshCount);

        Info = modelInfo;
        Bounds = bounds;
        Meshes = meshes;
        Animation = animation;
        _textures = modelInfo.TextureCount > 0 ? new Texture[modelInfo.TextureCount] : [];
        _materials = modelInfo.MaterialCount > 0 ? new Material[modelInfo.MaterialCount] : [];
    }


    public Material GetMaterial(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_materials.Length, nameof(index));
        return _materials[index];
    }
    
    public Texture GetTexture(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_textures.Length, nameof(index));
        return _textures[index];
    }
    
    public ReadOnlySpan<Texture> GetTextures() => _textures;
    public ReadOnlySpan<Material> GetMaterials() => _materials;
    
    internal void SetTexture(int index, Texture texture)
    {
        if ((uint)index >= (uint)_textures.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (_textures[index] != null) throw new InvalidOperationException($"Texture {index} already set.");
        _textures[index] = texture;
    }
    
    internal void SetMaterial(int index, Material texture)
    {
        if ((uint)index >= (uint)_materials.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (_materials[index] != null) throw new InvalidOperationException($"Material {index} already set.");
        _materials[index] = texture;
    }

}