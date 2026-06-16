using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MeshEntry(string name, MeshInfo info)
{
    public readonly string Name = name;
    public readonly MeshInfo Info = info;

    public MeshId MeshId { get; private set; }
    private Matrix4x4 _transform;
    private BoundingBox _bounds;

    public ref readonly Matrix4x4 Transform => ref _transform;
    public ref readonly BoundingBox Bounds => ref _bounds;

    internal void SetMeshId(MeshId meshId)
    {
        if (MeshId.IsValid() || !meshId.IsValid()) Throwers.InvalidOperation(nameof(MeshId));
        MeshId = meshId;
    }

    internal void SetTransform(in Matrix4x4 transform) => _transform = transform;
    internal void SetBounds(in BoundingBox bounds) => _bounds = bounds;
}

public sealed class Model : AssetObject
{
    public MeshEntry[] Meshes { get; }
    public ModelRig? Animation { get; }

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
        ReadOnlySpan<MeshEntry> meshes,
        ModelRig? animation) : base(name, id, gid)
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
        Meshes = meshes.ToArray();
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