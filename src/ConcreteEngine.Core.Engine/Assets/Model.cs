using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Assets;


public sealed class Model : AssetObject
{
    private readonly Mesh[] _meshes;
    private readonly Texture[] _textures;
    private readonly Material[] _materials;

    public readonly ModelRig? Rig;

    public readonly ModelInfo Info;
    public readonly BoundingBox Bounds;

    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;


    public Model(
        string name,
        AssetId id,
        Guid gid,
        in ModelInfo modelInfo,
        in BoundingBox bounds,
        ReadOnlySpan<Mesh> meshes,
        ModelRig? rig) : base(name, id, gid)
    {
        ArgumentOutOfRangeException.ThrowIfZero(meshes.Length, nameof(meshes));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meshes.Length, modelInfo.MeshCount, nameof(meshes));
        foreach (var mesh in meshes)
        {
            if (mesh == null! || !mesh.MeshId.IsValid() || mesh.Name == null!)
                Throwers.InvalidArgument(nameof(meshes));
        }

        Info = modelInfo;
        Bounds = bounds;
        _meshes = meshes.ToArray();
        Rig = rig;
        _textures = modelInfo.TextureCount > 0 ? new Texture[modelInfo.TextureCount] : [];
        _materials = modelInfo.MaterialCount > 0 ? new Material[modelInfo.MaterialCount] : [];
    }
    

    public ReadOnlySpan<Mesh> GetMeshes() => _meshes;
    public ReadOnlySpan<Texture> GetTextures() => _textures;
    public ReadOnlySpan<Material> GetMaterials() => _materials;

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
    
    public Mesh GetMesh(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_meshes.Length, nameof(index));
        return _meshes[index];
    }
    
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