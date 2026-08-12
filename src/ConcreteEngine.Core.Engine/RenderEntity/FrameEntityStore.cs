using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.RenderEntity;


public sealed class FrameEntityStore : IDisposable
{
    public int VisibleCount { get; private set; }

    private NativeArray<DrawEntityIndex> _commands;

    internal FrameEntityStore(int capacity)
    {
        _commands = NativeArray.Allocate<DrawEntityIndex>(capacity);
    }

    public NativeView<DrawEntityIndex> VisibleEntities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _commands.Slice(0, VisibleCount);
    }

    public ReadOnlySpan<DrawEntityIndex> VisibleSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _commands.AsReadOnlySpan(0, VisibleCount);
    }

    internal NativeView<DrawEntityIndex> WriteVisibleEntities()
    {
        if ((uint)RenderEcs.Core.Count > (uint)_commands.Length)
            Throwers.BufferOverflow(nameof(_commands));

        VisibleCount = 0;
        return _commands.Slice(0, RenderEcs.Core.Count);
    }


    internal void CommitFrame(int visibleCount)
    {
        if (visibleCount == 0)
        {
            VisibleCount = 0;
            return;
        }
        
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)visibleCount, (uint)_commands.Length);
        if (!_commands[visibleCount - 1].IsValid())
            Throwers.InvalidArgument(nameof(visibleCount));

        VisibleCount = visibleCount;
        VisibleEntities.AsSpan().Sort();

    }


    internal void Resize(int newSize)
    {
        if (newSize <= _commands.Length) return;
        _commands.ReAlloc(newSize, true);
    }


    public void Dispose() => _commands.Dispose();
}