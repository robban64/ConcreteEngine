using ConcreteEngine.Core.Common;

namespace ConcreteEngine.Engine.Render.Buffers;

public static class EffectBuffer
{
    private static byte _effectCount;
    private static EffectUniformParams[] _effects = new EffectUniformParams[16];

    public static void Reset() => _effectCount = 0;

    public static byte Submit(EffectUniformParams effect)
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

    public static ref EffectUniformParams Get(byte slot)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, _effects.Length);
        return ref _effects[slot];
    }
}