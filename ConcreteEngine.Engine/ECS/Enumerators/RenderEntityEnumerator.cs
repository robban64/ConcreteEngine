using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Engine.ECS.Enumerators;

internal ref struct RenderEntityEnumerator(RenderEntityContext view)
{
    private readonly RenderEntityContext _view = view;

    private int _i = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => ++_i < _view.Count;
    
    public readonly EntityCoreQuery Current => new(_i, _view);

    public RenderEntityEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }

    internal readonly ref struct EntityCoreQuery(int idx, RenderEntityContext view)
    {
        private readonly RenderEntityContext _view = view;
        public readonly int Index = idx;
        public readonly RenderEntityId RenderEntity = new(idx + 1);

        public ref SourceComponent Source
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _view.Sources[Index];
        }

        public ref Transform Transform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _view.Transforms[Index];
        }

        public ref BoxComponent Box
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _view.Boxes[Index];
        }
    }
}