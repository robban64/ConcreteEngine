using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Render.Data;

public readonly ref struct DrawEntityItem(
    int index,
    RenderEntityId entity,
    ref DrawCommand command,
    ref DrawCommandMeta meta)
{
    public readonly int Index = index;
    public readonly RenderEntityId Entity = entity;
    private readonly ref DrawCommand _commands = ref command;
    private readonly ref DrawCommandMeta _metas = ref meta;
    
    public ref DrawCommand Command
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _commands, Index);
    }

    public ref DrawCommandMeta Meta
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref _metas, Index);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal readonly ref struct DrawEntityContext(
    Span<RenderEntityId> visibleEntities,
    Span<int> visibleByIndices,
    UnsafeZippedSpan<DrawCommand, DrawCommandMeta> drawCommands)
{
    public readonly UnsafeSpan<RenderEntityId> VisibleEntities = new(visibleEntities);
    public readonly UnsafeSpan<int> VisibleIndices = new(visibleByIndices);
    public readonly UnsafeZippedSpan<DrawCommand, DrawCommandMeta> DrawCommands = drawCommands;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityItem TryGetVisible(RenderEntityId entity)
    {
        var index = VisibleIndices[entity.Index()];
        return index > 0
            ? new DrawEntityItem(index, entity, ref DrawCommands.Ref1, ref DrawCommands.Ref2)
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityEnumerator GetEnumerator() => new(VisibleEntities, DrawCommands);


    internal ref struct DrawEntityEnumerator(
        UnsafeSpan<RenderEntityId> visibleEntities,
        UnsafeZippedSpan<DrawCommand, DrawCommandMeta> upload)
    {
        private int _i = -1;
        private readonly int _length = visibleEntities.Length;
        private readonly ref RenderEntityId _visibleEntities = ref visibleEntities[0];
        private readonly ref DrawCommand _commands = ref upload.Ref1;
        private readonly ref DrawCommandMeta _metas = ref upload.Ref2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _length;

        public readonly DrawEntityItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, Unsafe.Add(ref _visibleEntities, _i), ref _commands, ref _metas);
        }

        public DrawEntityEnumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }
    }
}