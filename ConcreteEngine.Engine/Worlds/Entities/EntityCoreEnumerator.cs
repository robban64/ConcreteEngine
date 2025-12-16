using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Entities.Components;

namespace ConcreteEngine.Engine.Worlds.Entities;

internal ref struct EntityCoreEnumerator(EntitiesCoreView view)
{
    private readonly EntitiesCoreView _view = view;

    private int _i = -1;

    public bool MoveNext() => ++_i < _view.Count;
    public readonly EntityCoreQuery Current => new(_i, _view);

    public EntityCoreEnumerator GetEnumerator()
    {
        _i = -1;
        return this;
    }

    internal readonly ref struct EntityCoreQuery(int idx, EntitiesCoreView view)
    {
        private readonly EntitiesCoreView _view = view;
        public readonly int Index = idx;
        public readonly EntityHandle Entity = new(idx + 1);

        public ref RenderSourceComponent Source
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