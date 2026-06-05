using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Scene;

public abstract class BlueprintInstance
{
    public abstract SceneObjectBlueprint Blueprint { get; }

    public string DisplayName { get; protected set; }
    public bool IsDirty { get; private set; } = true;

    internal readonly List<RenderEntityId> RenderEntityIds = [];
    internal readonly List<GameEntityId> GameEntityIds = [];

    public bool HasRenderEcs => RenderEntityIds.Count > 0;
    public bool HasGameEntityIds => GameEntityIds.Count > 0;
    public bool IsMixedEcs => HasRenderEcs && HasGameEntityIds;
    
    public ReadOnlySpan<RenderEntityId> GetRenderEntities() => CollectionsMarshal.AsSpan(RenderEntityIds);
    public ReadOnlySpan<GameEntityId> GetGameEntities() => CollectionsMarshal.AsSpan(GameEntityIds);

    internal void Commit() => IsDirty = false;

    public void ToggleSelection(bool isSelected)
    {
        if (!HasRenderEcs) return;
        var selectionStore = Ecs.Render.Stores<SelectionComponent>.Store;

        foreach (var entity in GetRenderEntities())
        {
            if (isSelected) selectionStore.Add(entity, new SelectionComponent(SelectionComponent.DefaultHighlight));
            else selectionStore.Remove(entity);
        }
    }

    public void ToggleDebugBounds(bool isSelected)
    {
        if (!HasRenderEcs) return;
        var debugStore = Ecs.Render.Stores<DebugBoundsComponent>.Store;
        var isFirst = true;

        var span = GetRenderEntities();
        for (var i = 0; i < span.Length; i++)
        {
            var entity = span[i];
            var color = DebugBoundsComponent.DefaultColors[i % (DebugBoundsComponent.DefaultColors.Length - 1)];
            if (isSelected) debugStore.Add(entity, new DebugBoundsComponent(color));
            else debugStore.Remove(entity);
        }
    }
}


public sealed class ModelInstance : BlueprintInstance, IAssetListener<Model>, IAssetListener<Material>
{
    private readonly AssetRef<Model> _model;
    private readonly AssetRef<Material>[] _materials;
    
    public Transform LocalTransform;
    public BoundingBox LocalBounds;

    public ModelInstance(ModelBlueprint blueprint, Model model)
    {
        var materialCount = int.Max(model.Info.MaterialCount, blueprint.Materials.Length);
        
        Blueprint = blueprint;
        _model = new AssetRef<Model>(model, this);
        _materials = new AssetRef<Material>[materialCount];

        DisplayName = string.IsNullOrEmpty(blueprint.DisplayName) ? model.Name : blueprint.DisplayName;
        
        LocalTransform = blueprint.LocalTransform;
        LocalBounds = model.Bounds;
    }
    public override ModelBlueprint Blueprint { get; }

    public Model AssetModel => _model.Asset;
    public int MaterialCount => _materials.Length;

    public Material GetMaterial(int index)
    {
        if((uint)index >= (uint)_materials.Length) Throwers.InvalidArgument(nameof(index));
        return _materials[index].Asset;
    }

    public void SetMaterial(int index, Material material)
    {
        if (_materials[index] is {} currentMaterial)
        {
            if(currentMaterial.Asset == material) return;
            currentMaterial.Detach();
        }
        
        _materials[index] = new AssetRef<Material>(material, this);
    }

    public void OnChanged(Model model) {}
    public void OnRemoved(AssetRef<Model> modelRef) {}

    public void OnChanged(Material material)
    {
        var materialId = material.MaterialId;
        var drawQueue = material.State.DrawQueue;
        var passMask = material.State.PassMasks;
        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.Render.Core.GetSource(entity);
            if(source.Material != materialId) continue;
            source.Queue = drawQueue;
            source.Mask = passMask;
        }
    }

    public void OnRemoved(AssetRef<Material> materialRef)
    {
        var materialId = materialRef.Asset.MaterialId;
        var fallback = Material.FallbackMaterial;
        foreach (var entity in GetRenderEntities())
        {
            ref var source = ref Ecs.RenderCore.GetSource(entity);
            if (source.Material == materialId)
            {
                source.Material = fallback.MaterialId;
                source.Queue = fallback.State.DrawQueue;
                source.Mask = fallback.State.PassMasks;
            }
        }
        
        var idx = _materials.IndexOf(materialRef);
        _materials[idx] = new AssetRef<Material>(fallback, this);
    }

}

public sealed class AnimationInstance : BlueprintInstance
{
    public override ModelBlueprint Blueprint { get; }
    public ModelAnimation AssetAnimation { get; }
    public AnimationInstance(ModelBlueprint blueprint, ModelAnimation assetAnimation)
    {
        Blueprint = blueprint;
        AssetAnimation = assetAnimation;
        DisplayName = string.IsNullOrEmpty(blueprint.DisplayName) ?  "Animation" : blueprint.DisplayName;

    }

    public ref AnimationComponent GetComponent()
    {
        var store = Ecs.Game.Stores<AnimationComponent>.Store;
        foreach (var entity in GameEntityIds)
        {
            if (store.Has(entity)) return ref store.Get(entity);
        }

        throw new InvalidOperationException();
    }
}

public sealed class ParticleInstance : BlueprintInstance
{
    public override ParticleBlueprint Blueprint { get; }
    public ParticleEmitter Emitter { get; }
    public ParticleInstance(ParticleBlueprint blueprint, ParticleEmitter emitter)
    {
        Blueprint = blueprint;
        Emitter = emitter;
        DisplayName = string.IsNullOrEmpty(blueprint.DisplayName) ? emitter.Name : blueprint.DisplayName;

    }

}