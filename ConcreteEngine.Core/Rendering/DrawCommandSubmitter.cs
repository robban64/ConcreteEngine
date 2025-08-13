using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;

public sealed class DrawCommandSubmitter
{
    // 1. RenderTargetId        ex Scene
    // 2. DrawCommandMeta.Id    ex Tilemap
    // 3. DrawCommandMessage 
    private readonly DrawCommandMessage[][][] _commandQueues;
    private readonly int[][] _commandQueueIndexes;

    public DrawCommandSubmitter()
    {
        int renderTargetCount = Enum.GetValues<RenderTargetId>().Length;
        int drawCommandTypeCount = Enum.GetValues<DrawCommandId>().Length;
        _commandQueues = new DrawCommandMessage[renderTargetCount][][];
        _commandQueueIndexes = new int[renderTargetCount][];

        for (int i = 0; i < renderTargetCount; i++)
        {
            _commandQueueIndexes[i] = new int[drawCommandTypeCount];
            _commandQueues[i] = new DrawCommandMessage[drawCommandTypeCount][];

            for (int j = 0; j < drawCommandTypeCount; j++)
                _commandQueues[i][j] = [];
        }
    }

    public void RegisterCommand(DrawCommandId commandId, RenderTargetId target, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfZero(capacity, nameof(capacity));
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 4, nameof(capacity));

        if (_commandQueues[(int)target][(int)commandId].Length != 0)
            throw new InvalidOperationException(
                $"Command {Enum.GetName(commandId)} is already registered at target: {Enum.GetName(target)}");
        
        _commandQueues[(int)target][(int)commandId] = new DrawCommandMessage[capacity];
        _commandQueueIndexes[(int)target][(int)commandId] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubmitDraw(in DrawCommandData cmd, in DrawCommandMeta meta)
    {
        int target = (int)meta.Target;
        int commandId = (int)meta.Id;
        int index = _commandQueueIndexes[target][commandId]++;
        _commandQueues[target][commandId][index] = new DrawCommandMessage(in cmd, in meta);
    }

    public ReadOnlySpan<DrawCommandMessage> GetQueue(RenderTargetId target, DrawCommandId commandId)
    {
        var index = _commandQueueIndexes[(int)target][(int)commandId];
        return _commandQueues[(int)target][(int)commandId].AsSpan(0, index);
    }

    public void ResetBufferPointer()
    {
        for (int i = 0; i < _commandQueueIndexes.Length; i++)
        {
            for (int j = 0; j < _commandQueueIndexes[i].Length; j++)
            {
                _commandQueueIndexes[i][j] = 0;
            }
        }
    }
}