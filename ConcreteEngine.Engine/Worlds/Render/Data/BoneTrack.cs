using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal readonly ref struct BoneTrackView(int length, KeyFrameVec3[] positions, KeyFrameQuat[] rotations)
{
    public ReadOnlySpan<KeyFrameVec3> Positions => positions;
    public ReadOnlySpan<KeyFrameQuat> Rotations => rotations;

    public readonly int Length = length;
}

internal readonly struct BoneTrack
{
    private readonly KeyFrameVec3[] _positions;

    private readonly KeyFrameQuat[] _rotations;

    //private readonly KeyFrameVec3[] _scales;
    private readonly int _length;

    public BoneTrackView GetTrackView() => new(_length, _positions, _rotations);

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length == 0;
    }

    public ReadOnlySpan<KeyFrameVec3> Positions => _positions;
    public ReadOnlySpan<KeyFrameQuat> Rotations => _rotations;
    //public ReadOnlySpan<KeyFrameVec3> Scales => _scales;

    public BoneTrack()
    {
        _positions = [];
        _rotations = [];
        //_scales = [];
        _length = 0;
    }

    public BoneTrack(
        ReadOnlySpan<Vector3> pSpan,
        ReadOnlySpan<float> pTimes,
        ReadOnlySpan<Quaternion> rSpan,
        ReadOnlySpan<float> rTimes,
        ReadOnlySpan<Vector3> sSpan,
        ReadOnlySpan<float> sTimes)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(pSpan.Length, pTimes.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(rSpan.Length, rTimes.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(sSpan.Length, sTimes.Length);

        var translations = new KeyFrameVec3[pSpan.Length];
        var rotations = new KeyFrameQuat[rSpan.Length];
        //var scales = new KeyFrameVec3[sSpan.Length];

        _length = int.Max(pSpan.Length, int.Max(rSpan.Length, sSpan.Length));

        for (int i = 0; i < pSpan.Length; i++)
            translations[i] = new KeyFrameVec3(pTimes[i], pSpan[i]);

        for (int i = 0; i < rSpan.Length; i++)
            rotations[i] = new KeyFrameQuat(rTimes[i], rSpan[i]);

        /*
         for (int i = 0; i < sSpan.Length; i++)
            scales[i] = new KeyFrameVec3(sTimes[i], sSpan[i]);
        */

        _positions = translations;
        _rotations = rotations;
        //_scales = scales;
    }
}

public interface IKeyFrame
{
    float Time { get; }
}

public readonly struct KeyFrameVec3(float time, Vector3 value) : IKeyFrame
{
    public readonly Vector3 Value = value;
    public float Time { get; } = time;
}

public readonly struct KeyFrameQuat(float time, Quaternion value) : IKeyFrame
{
    public readonly Quaternion Value = value;
    public float Time { get; } = time;
}