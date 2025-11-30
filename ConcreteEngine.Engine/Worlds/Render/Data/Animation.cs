using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

public readonly struct BoneTrack
{
    private readonly Vector3Key[] _translations;
    private readonly QuaternionKey[] _rotations;
    private readonly Vector3Key[] _scales;
    private readonly int _length;

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length == 0;
    }

    public ReadOnlySpan<Vector3Key> Translations => _translations;
    public ReadOnlySpan<QuaternionKey> Rotations => _rotations;
    public ReadOnlySpan<Vector3Key> Scales => _scales;

    public BoneTrack()
    {
        _translations = [];
        _rotations = [];
        _scales = [];
        _length = 0;
    }

    public BoneTrack(
        ReadOnlySpan<Vector3> tSpan,
        ReadOnlySpan<float> tTimes,
        ReadOnlySpan<Quaternion> rSpan,
        ReadOnlySpan<float> rTimes,
        ReadOnlySpan<Vector3> sSpan,
        ReadOnlySpan<float> sTimes)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(tSpan.Length, tTimes.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(rSpan.Length, rTimes.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(sSpan.Length, sTimes.Length);

        var translations = new Vector3Key[tSpan.Length];
        var rotations = new QuaternionKey[rSpan.Length];
        var scales = new Vector3Key[sSpan.Length];

        _length = int.Max(tSpan.Length, int.Max(rSpan.Length, sSpan.Length));

        for (int i = 0; i < tSpan.Length; i++)
            translations[i] = new Vector3Key(tTimes[i], tSpan[i]);

        for (int i = 0; i < rSpan.Length; i++)
            rotations[i] = new QuaternionKey(rTimes[i], rSpan[i]);

        for (int i = 0; i < sSpan.Length; i++)
            scales[i] = new Vector3Key(sTimes[i], sSpan[i]);

        _translations = translations;
        _rotations = rotations;
        _scales = scales;
    }
}

public interface IKeyFrame
{
    float Time { get; }
}

public readonly struct Vector3Key(float time, Vector3 value) : IKeyFrame
{
    public float Time { get; } = time;
    public readonly Vector3 Value = value;
}

public readonly struct QuaternionKey(float time, Quaternion value) : IKeyFrame
{
    public float Time { get; } = time;
    public readonly Quaternion Value = value;
}