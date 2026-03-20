using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Data;

public readonly ref struct DrawEntityItem(
    int index,
    RenderEntityId entity,
    UnsafeZippedSpan<DrawCommand, DrawCommandMeta> upload)
{
    public readonly int Index = index;
    public readonly RenderEntityId Entity = entity;
    private readonly UnsafeZippedSpan<DrawCommand, DrawCommandMeta> _upload = upload;

    public ref DrawCommand Command
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _upload.At1(Index);
    }

    public ref DrawCommandMeta Meta
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _upload.At2(Index);
    }
}

internal readonly ref struct DrawEntityContext
{
    private readonly UnsafeSpan<RenderEntityId> _visibleEntities;
    private readonly UnsafeSpan<int> _visibleIndices;
    private readonly UnsafeZippedSpan<DrawCommand, DrawCommandMeta> _commandZip;
    
    public DrawEntityContext(
         Span<RenderEntityId> visibleEntities,
         Span<int> visibleByIndices,
         DrawCommandBuffer commandBuffer)
    {
        ArgumentNullException.ThrowIfNull(commandBuffer);
        
        _visibleEntities = new UnsafeSpan<RenderEntityId>(visibleEntities);
        _visibleIndices = new UnsafeSpan<int>(visibleByIndices);
        _commandZip = commandBuffer.GetDrawCommands();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityItem TryGetVisible(RenderEntityId entity)
    {
        var index = _visibleIndices[entity.Index()];
        return index < 0 ? default : new DrawEntityItem(index,entity, _commandZip);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawEntityEnumerator GetEnumerator() => new(_visibleEntities, _commandZip);


    internal ref struct DrawEntityEnumerator(
        UnsafeSpan<RenderEntityId> visibleEntities,
        UnsafeZippedSpan<DrawCommand, DrawCommandMeta> upload)
    {
        private readonly UnsafeSpan<RenderEntityId> _visibleEntities = visibleEntities;
        private readonly UnsafeZippedSpan<DrawCommand, DrawCommandMeta> _upload = upload;
        private int _i = -1;
        private readonly int _length = visibleEntities.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_i < _length;

        public readonly DrawEntityItem Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_i, _visibleEntities[_i], _upload);
        }

        public DrawEntityEnumerator GetEnumerator()
        {
            _i = -1;
            return this;
        }
    }
}