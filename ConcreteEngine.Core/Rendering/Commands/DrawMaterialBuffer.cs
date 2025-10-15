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
    private const int DispatchStackSize = 16;
    
    private DrawMaterialCommandRef[] _commands = new DrawMaterialCommandRef[DefaultMaterialBufferCapacity];
    private MaterialParams[] _buffer = new MaterialParams[DefaultMaterialBufferCapacity];
    
    private int _submitIdx = 0;

    private DrawProcessor _drawProcessor;

    internal DrawMaterialBuffer(DrawProcessor drawProcessor)
    {
        _drawProcessor = drawProcessor;
    }

    public void SubmitMaterials(ReadOnlySpan<DrawMaterialCommand> cmdSpan, ReadOnlySpan<MaterialParams> dataSpan)
    {
        Debug.Assert(cmdSpan.Length == dataSpan.Length);
        EnsureCapacity(cmdSpan.Length);
        
        dataSpan.CopyTo(_buffer.AsSpan(_submitIdx));

        for (var i = 0; i < cmdSpan.Length; i++)
        {
            var idx = _submitIdx + i;
            _commands[idx] = new DrawMaterialCommandRef(cmdSpan[i], idx);
        }
    }

    public void DispatchMaterials()
    {
        Debug.Assert(_commands.Length == _buffer.Length);
        if(_submitIdx == 0 || _commands.Length == 0) return;
        if (_submitIdx == 1)
        {
            var data = new MaterialUniformRecord(in _buffer[0]);
            _drawProcessor.UploadMaterialRecord(_commands[0].MaterialId, data);
            return;
        }
        
        var commands = _commands.AsSpan(0, _submitIdx);
        var payloads = _buffer.AsSpan(0, _submitIdx);
        
        commands.Sort();

        var uploadBufferSize = int.Min(commands.Length, DispatchStackSize);
        
        Span<MaterialId> materialIdBuffer = stackalloc MaterialId[uploadBufferSize];
        Span<MaterialUniformRecord> uploadBuffer = stackalloc MaterialUniformRecord[uploadBufferSize];
        
        var cursor = 0;
        for (var i = 0; i < _submitIdx; i++)
        {
            var cmd = commands[i];
            ref readonly var payload = ref payloads[cmd.SubmitIdx];
            uploadBuffer[cursor] = new MaterialUniformRecord(in payload);
            materialIdBuffer[cursor++] = cmd.MaterialId;
            
            if (cursor >= uploadBufferSize)
            {
                // Dispatch
                _drawProcessor.UploadMaterialSpan(materialIdBuffer, uploadBuffer);
                cursor = 0;
            }
        }
    }

    public void Reset()
    {
        _submitIdx = 0;
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