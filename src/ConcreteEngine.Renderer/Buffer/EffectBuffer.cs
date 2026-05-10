using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Renderer.Visuals;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class EffectBuffer
{
    private byte _effectCount;
    private ResolveEffectParams[] _resolveEffects = new ResolveEffectParams[16];

    public void Reset() => _effectCount = 0;

    public byte SubmitResolveEffect(ResolveEffectParams effect)
    {
        var index = _effectCount++;
        if (index >= _resolveEffects.Length)
        {
            var newCap = _resolveEffects.Length * 2;
            if (newCap >= 255) Throwers.ThrowBufferFull(nameof(EffectBuffer), newCap, 255);
            Array.Resize(ref _resolveEffects, newCap);
        }

        _resolveEffects[index] = effect;
        return index;
    }

    public ref ResolveEffectParams GetResolveEffect(byte slot)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _resolveEffects.Length);
        return ref _resolveEffects[slot];
    }
}