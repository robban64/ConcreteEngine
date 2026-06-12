using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.Graphics;

internal sealed class AnimationManager
{
    internal static readonly AnimationManager Instance = new();

    private ModelRig[] _rigs = [];
    private readonly List<AnimationRigInstance> _animations = new(4);
    
    public int RigCount { get; private set; }
    public int AnimationCount => _animations.Count;

    private AnimationManager() { }
    
    internal void Simulate(float dt)
    {
        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }
    
    public void AttachEntity(Id16<ModelRig> rigId, ushort rigInstanceId, RenderEntityId entity)
    {
        var instance = GetAnimationInstance(rigId, rigInstanceId);

        if (instance is null)
        {
            var clip = GetRig(rigId).Clips[0];
            var gameComponent = new AnimationComponent(rigId) { Duration = clip.Duration, Speed = clip.TicksPerSecond };

            var animationEntity = Ecs.GameCore.AddEntity();
            Ecs.GetGameStore<AnimationComponent>().Add(animationEntity, gameComponent);
            instance = new AnimationRigInstance(rigId, rigInstanceId, animationEntity);
            _animations.Add(instance);
        }

        instance.RenderEntities.Add(entity);
        Ecs.GetRenderStore<SkinningComponent>().Add(entity, new SkinningComponent(instance.AnimationEntity));
    }


    private AnimationRigInstance? GetAnimationInstance(Id16<ModelRig> rigId, ushort rigInstanceId)
    {
        foreach (var a in _animations)
        {
            if (a.RigId == rigId && a.RigInstanceId == rigInstanceId) return a;
        }
        return  null;
    }

    internal ModelRig GetRig(Id16<ModelRig> id)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_rigs.Length) Throwers.IndexOutOfRange(nameof(ModelRig), index, _rigs.Length);
        return _rigs[index];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SkinningContext GetSkinningContext(Id16<ModelRig> id, int clip)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_rigs.Length) Throwers.IndexOutOfRange(nameof(ModelRig), index, _rigs.Length);
        return _rigs[index].GetSkinningContext(clip);
    }

    
    internal void Setup(AssetStore assets)
    {
        int count = 0;
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            count++;
        }

        RigCount = count;
        _rigs = new ModelRig[count];
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is not { } rig) continue;
            _rigs[rig.Id.Index()] = rig;
        }
    }

    private sealed class AnimationRigInstance : IComparable<AnimationRigInstance>
    {
        public readonly ushort RigInstanceId;
        public readonly Id16<ModelRig> RigId;
        public readonly GameEntityId AnimationEntity;

        internal readonly List<RenderEntityId> RenderEntities = [];

        public AnimationRigInstance(Id16<ModelRig> rigId, ushort rigInstanceId, GameEntityId animationEntity)
        {
            ArgumentOutOfRangeException.ThrowIfZero(rigId.Value, nameof(rigId));
            ArgumentOutOfRangeException.ThrowIfZero(animationEntity.Id, nameof(animationEntity));
            RigInstanceId = rigInstanceId;
            RigId = rigId;
            AnimationEntity = animationEntity;
        }

        public int CompareTo(AnimationRigInstance? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var c = RigInstanceId.CompareTo(other.RigInstanceId);
            return c != 0 ? c : RigId.CompareTo(other.RigId);
        }
    }
}