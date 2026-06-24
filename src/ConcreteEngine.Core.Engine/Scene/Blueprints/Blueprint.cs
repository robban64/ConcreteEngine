using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Core.Engine.Scene;

public interface IBlueprint
{
    string DisplayName { get; }
    Guid GId { get; }
}

public abstract class GameBlueprint : IBlueprint
{
    public bool IsDirty { get; internal set; }

    public string DisplayName { get; set; } = string.Empty;
    public Guid GId { get; } = Guid.NewGuid();

    private readonly List<GameBlueprintInstance> _instances = [];
    public ReadOnlySpan<GameBlueprintInstance> GetInstanceSpan() => CollectionsMarshal.AsSpan(_instances);

    public void AddInstance(GameBlueprintInstance instance)
    {
        if (_instances.Contains(instance)) return;
        _instances.Add(instance);
    }

    public void RemoveInstance(GameBlueprintInstance instance)
    {
        _instances.Remove(instance);
    }

    private void NotifyChanges()
    {
        foreach (var instance in GetInstanceSpan())
        {
            instance.MarkDirty(SceneDirtyFlags.Blueprint);
        }
    }
}

public abstract class RenderBlueprint : IBlueprint, IAssetListener
{
    public bool IsDirty { get; internal set; }

    public string DisplayName { get; set; } = string.Empty;
    public Guid GId { get; } = Guid.NewGuid();

    public Transform LocalTransform = Transform.Identity;

    protected readonly AssetRef<Material>?[] Materials;

    private readonly List<RenderBlueprintInstance> _instances = [];

    protected RenderBlueprint(int materialCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(materialCount);
        Materials = new AssetRef<Material>[materialCount];
    }

    public int MaterialCount => Materials.Length;
    public ReadOnlySpan<RenderBlueprintInstance> GetInstanceSpan() => CollectionsMarshal.AsSpan(_instances);

    public void AddInstance(RenderBlueprintInstance instance)
    {
        if (_instances.Contains(instance)) return;
        _instances.Add(instance);
    }

    public void RemoveInstance(RenderBlueprintInstance instance)
    {
        _instances.Remove(instance);
    }

    private void NotifyChanges()
    {
        foreach (var instance in GetInstanceSpan())
        {
            instance.MarkDirty(SceneDirtyFlags.Blueprint);
        }
    }


    public Material GetMaterial(int index)
    {
        if ((uint)index >= (uint)Materials.Length) Throwers.InvalidArgument(nameof(index));
        var material = Materials[index];
        return material is null ? AssetStore.Core.FallbackMaterial : material.Asset;
    }

    public void SetMaterial(int index, Material material)
    {
        if (Materials[index] is { } currentMaterial)
        {
            if (currentMaterial.Asset == material) return;
            currentMaterial.Detach();
        }

        Materials[index] = new AssetRef<Material>(material, this);
    }


    public void OnAssetChanged(AssetObject asset)
    {
        if (asset is not Material material || (material.DirtyFlags & AssetDirtyFlag.Structure) == 0) return;
        foreach (var instance in GetInstanceSpan())
            instance.ApplyMaterial(material.State);
    }

    public void OnAssetRemoved(AssetObject asset)
    {
        if (asset is not Material) return;
        foreach (var instance in GetInstanceSpan())
            instance.ApplyMaterial(AssetStore.Core.FallbackMaterial.State);
    }
}