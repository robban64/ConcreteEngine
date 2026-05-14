using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Renderer.Buffer;

public sealed class EffectBuffer
{
    private byte _effectCount;
    private EffectUniformParams[] _effects = new EffectUniformParams[16];

    public void Reset() => _effectCount = 0;

    public byte Submit(EffectUniformParams effect)
    {
        var index = _effectCount++;
        if (index >= _effects.Length)
        {
            var newCap = _effects.Length * 2;
            if (newCap >= 255) Throwers.BufferOverflow(nameof(EffectBuffer), newCap, 255);
            Array.Resize(ref _effects, newCap);
        }

        _effects[index] = effect;
        return index;
    }

    public ref EffectUniformParams Get(byte slot)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _effects.Length);
        return ref _effects[slot];
    }
}