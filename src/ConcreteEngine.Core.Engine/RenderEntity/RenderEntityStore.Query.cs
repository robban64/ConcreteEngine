using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Core.Engine.RenderEntity;


public sealed unsafe partial class RenderEntityStore<T> where T : unmanaged, IRenderComponent<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VisibilityEnumerator VisibilityQuery() => new(this, RenderEcs.Core);

    public readonly ref struct RenderQueryItem(int idx, RenderEntityId entityId, ref T component)
    {
        public readonly ref T Component = ref component;
        public readonly int Index = idx;
        public readonly RenderEntityId Entity = entityId;
    }

    public ref struct Enumerator(RenderEntityStore<T> store)
    {
        private int _i = -1;
        private RenderEntityId _currentEntity;
        private readonly int _count = store.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.IsValid())
                {
                    _currentEntity = entity;
                    return true;
                }
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, _currentEntity, ref store.GetByIndex(_i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() => this;
    }


    public ref struct VisibilityEnumerator(RenderEntityStore<T> store, RenderEntityCore core)
    {
        private int _i = -1;
        private RenderEntityId _currentEntity;
        private readonly int _count = store.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (++_i < _count)
            {
                var entity = store.GetEntity(_i);
                if (entity.Id > 0 && core.IsVisible(entity))
                {
                    _currentEntity = entity;
                    return true;
                }
            }

            return false;
        }

        public readonly RenderQueryItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, _currentEntity, ref store.GetByIndex(_i));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VisibilityEnumerator GetEnumerator() => this;
    }
}