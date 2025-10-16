using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Draw;
using ConcreteEngine.Core.Rendering.Registry;
using static ConcreteEngine.Core.Rendering.Data.RenderLimits;

namespace ConcreteEngine.Core.Rendering.Commands;

internal sealed class DrawMaterialBuffer
{
    
    private readonly DrawProcessor _drawProcessor;

    private int _submitIdx = 0;

    private DrawMaterialCommand[] _commands = new DrawMaterialCommand[DefaultMaterialBufferCapacity];
    private MaterialUniformRecord[] _buffer = new MaterialUniformRecord[DefaultMaterialBufferCapacity];
    
    public int Count => _submitIdx;

    internal DrawMaterialBuffer(DrawProcessor drawProcessor)
    {
        _drawProcessor = drawProcessor;
    }

    internal void Reset() => _submitIdx = 0;
    
    public void SubmitMaterials(ReadOnlySpan<DrawMaterialCommand> cmdSpan, ReadOnlySpan<MaterialParams> dataSpan)
    {
        Debug.Assert(cmdSpan.Length == dataSpan.Length);
        EnsureCapacity(cmdSpan.Length);
        
        cmdSpan.CopyTo(_commands.AsSpan(_submitIdx));

        for (var i = 0; i < cmdSpan.Length; i++)
            _buffer[_submitIdx + i] = new MaterialUniformRecord(dataSpan[i]);
        
        _submitIdx += cmdSpan.Length;
        
    }

    internal void DispatchMaterials()
    {
        Debug.Assert(_commands.Length == _buffer.Length);
        if(_submitIdx == 0) return;
        if (_submitIdx == 1)
        {
            _drawProcessor.UploadMaterialRecord(_commands[0].MaterialId, in _buffer[0] );
            return;
        }
        
        var commands = _commands.AsSpan(0, _submitIdx);
        var payloads = _buffer.AsSpan(0, _submitIdx);
        _drawProcessor.UploadMaterial(commands, payloads);
    }


    private void EnsureCapacity(int amount)
    {
        var idx = _submitIdx + amount;
        if (_commands.Length >= idx) return;
        var newCap = ArrayUtility.CapacityGrowthToFit(idx,Math.Max(idx, 4));

        if (newCap > MaxMaterialBufferCapacity)
            ThrowMaxCapacityExceeded();
        
        Array.Resize(ref _commands, newCap);
        Array.Resize(ref _buffer, newCap);

    }
    
    [MethodImpl(MethodImplOptions.NoInlining), DoesNotReturn, StackTraceHidden]
    private static void ThrowMaxCapacityExceeded() => throw new OutOfMemoryException("Material Buffer exceeded max limit");

}