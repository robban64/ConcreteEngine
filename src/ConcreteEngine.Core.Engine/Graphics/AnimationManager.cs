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
    public int Count { get; private set; }

    private ModelRig[] _animations = [];
    private readonly List<AnimationRigInstance> _animationInstances = new(4);

    private AnimationManager() { }

    public void AddAnimation(Id16<ModelRig> rigId, ushort rigInstanceId, RenderEntityId entity)
    {
        GameEntityId animationEntity = default;
        foreach (var a in _animationInstances)
        {
            if (a.RigId == rigId && a.RigInstanceId == rigInstanceId) animationEntity = a.AnimationEntity;
        }

        if (animationEntity == default)
        {
            animationEntity = Ecs.GameCore.AddEntity();
            
            var clip = _animations[rigId.Index()].Clips[0];
            var gameComponent = new AnimationComponent(rigId) 
                { Duration = clip.Duration, Speed = clip.TicksPerSecond };
            
            Ecs.GetGameStore<AnimationComponent>().Add(animationEntity,gameComponent);
            
            _animationInstances.Add(new AnimationRigInstance(rigId, rigInstanceId, animationEntity));
        }

        Ecs.GetRenderStore<SkinningComponent>().Add(entity, new SkinningComponent(animationEntity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SkinningContext GetSkinningContext(Id16<ModelRig> id, int clip)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_animations.Length)
            Throwers.IndexOutOfRange(nameof(ModelRig), index, _animations.Length);
        return _animations[index].GetSkinningContext(clip);
    }

    public ModelRig GetRig(Id16<ModelRig> id)
    {
        var index = id.Index();
        if ((uint)index >= (uint)_animations.Length)
            Throwers.IndexOutOfRange(nameof(ModelRig), index, _animations.Length);
        return _animations[index];
    }

    internal void Simulate(float dt)
    {
        foreach (var query in Ecs.Game.Query<AnimationComponent>())
        {
            ref var c = ref query.Component;
            c.AdvanceTime(dt);
        }
    }


    internal void Setup(AssetStore assets)
    {
        int count = 0;
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is null) continue;
            count++;
        }

        Count = count;
        _animations = new ModelRig[count];
        foreach (var model in assets.GetAssetEnumerator<Model>())
        {
            if (model.Animation is not { } rig) continue;
            _animations[rig.Id.Index()] = rig;
        }
    }
    
    private sealed class AnimationRigInstance(Id16<ModelRig> rigId, ushort rigInstanceId, GameEntityId animationEntity)
        : IComparable<AnimationRigInstance>
    {
        public readonly Id16<ModelRig> RigId = rigId;
        public readonly ushort RigInstanceId = rigInstanceId;
        public readonly GameEntityId AnimationEntity = animationEntity;

        public int CompareTo(AnimationRigInstance? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            var c = RigInstanceId.CompareTo(other.RigInstanceId);
            return c != 0 ? c : RigId.CompareTo(other.RigId);
        }
    }
}