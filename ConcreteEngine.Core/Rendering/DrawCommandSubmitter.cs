#region

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Definitions;
using static ConcreteEngine.Core.Rendering.RenderConsts;

#endregion

namespace ConcreteEngine.Core.Rendering;


interface IDrawCommandTargetQueue
{
    IReadOnlyList<DrawCommandId> CommandIds { get; }
    int Count { get; }
    void Reset();
}

public sealed class DrawCommandSubmitter
{
    private const int DefaultQueueCapacity = 32;
    private delegate void SubmitDelegate<T>(DrawCommandSubmitter queue, in T cmd, in DrawCommandMeta meta, int idx)
        where T : struct, IDrawCommand;

    private delegate void DrainDelegate(DrawCommandSubmitter queue, int layer);


    private static class Slot<T> where T : struct, IDrawCommand
    {
        internal static DrawCommandTargetQueue<T>? Queue;

        internal static readonly SubmitDelegate<T> SubmitDraw =
            static (DrawCommandSubmitter _, in T cmd, in DrawCommandMeta meta, int idx) =>
                Queue!.Enqueue(in cmd, in meta, idx);
        
        /*internal static readonly DrainDelegate Drain = 
        static (DrawCommandSubmitter _, int layer) =>*/
        
    }
    private static FieldInfo EnsureQueueField(Type tReq)
        => typeof(Slot<>).MakeGenericType(tReq)
            .GetField("Queue", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;


    private readonly List<CommandSegment>[] _passSegments;
    private readonly List<IDrawCommandTargetQueue> _queues;

    // key = DrawCommandId
    private readonly DrawCommandTag[] cmdRendererRegistry;
    

    private int _size = 0;

    public DrawCommandSubmitter()
    {
        _queues = new List<IDrawCommandTargetQueue>(4);
        cmdRendererRegistry = new DrawCommandTag[Enum.GetValues<DrawCommandId>().Length];
        _passSegments = new List<CommandSegment>[Enum.GetValues<RenderTargetId>().Length];
        for (int i = 0; i < _passSegments.Length; i++)
            _passSegments[i] = new List<CommandSegment>(8);
    }

    public void Register(Type cmdType, DrawCommandId cmdId)
    {
        var queueType = typeof(DrawCommandTargetQueue<>).MakeGenericType(cmdType);
        var queue = Activator.CreateInstance(queueType);

        Type[] argTypes = [typeof(DrawCommandSubmitter), cmdType.MakeByRefType(), typeof(DrawCommandMeta), typeof(int)];

        // Get 'void Enqueue(in TReq)' MethodInfo
        var mi = queueType.GetMethod(
            "Enqueue",
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [cmdType.MakeByRefType()], // 'in' shows as ByRef in reflection
            modifiers: null);

        // Build an open delegate: (Receiver r, in TReq req) => ((ReceiverQueue<TReq>)q).Enqueue(in req)
        var delType = typeof(SubmitDelegate<>).MakeGenericType(cmdType);
        var dm = new DynamicMethod(
            name: "Enq_" + cmdType.Name,
            returnType: typeof(void),
            parameterTypes: argTypes,
            m: typeof(DrawCommandSubmitter).Module, skipVisibility: true);

        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldsfld, EnsureQueueField(cmdType)); // or emit a constant with 'q'
        il.Emit(OpCodes.Ldarg_1);                        // push 'in TReq' (byref)
        il.Emit(OpCodes.Callvirt, mi);                   // call Enqueue(in TReq)
        il.Emit(OpCodes.Ret);

        var enq = dm.CreateDelegate(delType);

        // Assign strongly-typed fields: Slot<TReq>.Queue / .Enqueue
        typeof(DrawCommandSubmitter)
            .GetMethod(nameof(Register), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(cmdType)
            .Invoke(this, [queue, enq]);
    }
    
    public void Register<T>(DrawCommandId commandId) where T : struct, IDrawCommand
    {
        if (commandId == DrawCommandId.Invalid) throw new ArgumentException("Invalid command id", nameof(commandId));
        if (cmdRendererRegistry[(int)commandId] != DrawCommandTag.Invalid)
            throw new InvalidOperationException(
                $"Command: {Enum.GetName(commandId)} is already registered at a renderer: {typeof(T).Name}");

        if (Slot<T>.Queue == null)
        {
            var queue = new DrawCommandTargetQueue<T>(DefaultQueueCapacity);
            _queues.Add(queue);
            Slot<T>.Queue = queue;
        }
        Slot<T>.Queue!.RegisterCommand(commandId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : struct, IDrawCommand
    {
        Slot<T>.SubmitDraw(this, in cmd, in meta, _size++);
    }

    public ReadOnlySpan<CommandContainer<T>> DrainCommandQueue<T>() where T : struct, IDrawCommand
    {
        return Slot<T>.Queue!.DrainQueue();
    }


    public void Reset()
    {
        foreach (var queue in _queues)
            queue.Reset();

        _size = 0;
    }

/*
    public ReadOnlySpan<CommandSegment> DrainCommandQueue()
    {
        var metaQueue = _metaQueue.DrainMetaQueue();
        metaQueue.Sort();

        int start = 0, end = metaQueue.Length;
        var previousTarget = RenderTargetId.Scene;

        for (short i = 0; i < end; i++)
        {
            ref readonly var meta = ref metaQueue[i];
            if (previousTarget == meta.Target) continue;
            var segment = new CommandSegment(meta.Target, meta.Tag, meta.Layer, (short)start, (short)(i - start + 1));
            _segments.Add(segment);
            previousTarget = meta.Tag;
            start = i;
        }


        var previous
        for (int i = 0; i < _segments.Count; i++)
        {
            var segment = _segments[i];
            segment.
        }
    }
*/

/*
    private ReadOnlySpan<CommandSegment> BuildSegmentsBucket(RenderTargetId target, int layer)
    {
        var meshQueue = _meshQueue.DrainQueue();
        var metaQueue = _metaQueue.DrainMetaQueue();
        var end = metaQueue.Length;
        int offset = 0;
        int emitted = 0;
        for (int i = 0; i < end; i++)
        {
            ref readonly var meta = ref metaQueue[i];
            if (meta.Target == target && meta.Layer == layer) continue;
            int length = i - offset + 1;
            var a = new CommandSegment(meta.Target, meta.Layer, meta.Tag, (short)offset, (short)length);


            emitted++;
            offset = i + 1;
        }
    }
*/

}

internal sealed class DrawCommandTargetQueue<T> : IDrawCommandTargetQueue where T : struct, IDrawCommand
{
    private readonly List<CommandContainer<T>> _queue;
    private readonly List<DrawCommandId> _commandIds = new();
    public IReadOnlyList<DrawCommandId> CommandIds => _commandIds;
    public int Count => _queue.Count;

    public DrawCommandTargetQueue(int capacity)
    {
        _queue = new List<CommandContainer<T>>(capacity);
    }

    public void RegisterCommand(DrawCommandId cmdId)
    {
        if(_commandIds.Contains(cmdId)) 
            throw new InvalidOperationException($"Command {cmdId} is already registered at a DrawCommandQueue: {typeof(DrawCommandTargetQueue<T>).Name}");
        _commandIds.Add(cmdId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(in T cmd, in DrawCommandMeta meta, int idx)
    {
        _queue.Add(new CommandContainer<T>(in cmd, in meta, idx));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<CommandContainer<T>> DrainQueue()
    {
        var span = CollectionsMarshal.AsSpan(_queue);
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _queue.Clear();
    }
}

internal sealed class DrawCommandMetaQueue
{
    private readonly DrawCommandMetaRef[] _queue;
    private int _idx = 0;
    public int Count => _idx;

    public DrawCommandMetaQueue(int capacity)
    {
        _queue = new DrawCommandMetaRef[capacity];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(in DrawCommandMeta meta, int cmdIdx)
    {
        _queue[_idx++] = new DrawCommandMetaRef(meta.Tag, meta.Target, meta.Layer, cmdIdx);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<DrawCommandMetaRef> DrainMetaQueue()
    {
        return _queue.AsSpan(0, _idx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset() => _idx = 0;
}


public readonly record struct DrawCommandMetaRef(
    DrawCommandTag Tag,
    RenderTargetId Target,
    byte Layer,
    int Index) : IComparable<DrawCommandMetaRef>
{
    public readonly uint Key24 = ((uint)(byte)Target << 16) | ((uint)Tag << 8) | (byte)Layer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandMetaRef other) => Key24.CompareTo(other.Key24);
}

public readonly record struct CommandSegment(
    RenderTargetId Target,
    DrawCommandTag Tag,
    byte Layer,
    short Start,
    short Length);
