using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class ModelAnimation
{
    [Inspectable] public readonly int AnimationCount;
    [Inspectable] public readonly List<AnimationClip> Clips;
    [Inspectable] public readonly Dictionary<string, int> BoneMapping;

    public readonly SkeletonData SkeletonData;

    public int BoneCount => BoneMapping.Count;

    public ModelAnimation(int animationCount, Dictionary<string, int> boneMapping)
    {
        AnimationCount = animationCount;

        BoneMapping = boneMapping;
        SkeletonData = new SkeletonData(boneMapping.Count);
        Clips = new List<AnimationClip>(animationCount);

        Array.Fill(SkeletonData.ParentIndices, -1);
    }
}

public sealed class AnimationClip
{
    public string Name;
    public float Duration;
    public float TicksPerSecond;

    public int Length => Channels.Length;

    public readonly AnimationChannel[] Channels;

    public AnimationClip(string name, int boneCount, float duration, float ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boneCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        Name = name;
        Channels = new AnimationChannel[boneCount];
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
    }
}

//TODO
public readonly struct SkeletonData
{
    public readonly int[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    public readonly int Length;

    public SkeletonData(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        Length = length;
        ParentIndices = new int[length];
        BindPose = new Matrix4x4[length];
        InverseBindPose = new Matrix4x4[length];
    }
}

//TODO
public readonly struct AnimationChannel(int positionLength, int rotationLength)
{
    public readonly float[] PositionTimes = new float[positionLength];
    public readonly Vector3[] Positions = new Vector3[positionLength];

    public readonly float[] RotationTimes = new float[rotationLength];
    public readonly Quaternion[] Rotations = new Quaternion[rotationLength];

    public readonly int MaxLength = int.Max(positionLength, rotationLength);

    public TrackView GetTrackView() => new(PositionTimes, Positions, RotationTimes, Rotations, MaxLength);

    public readonly ref struct TrackView(
        float[] positionTimes,
        Vector3[] positions,
        float[] rotationTimes,
        Quaternion[] rotations,
        int length)
    {
        public readonly UnsafeZippedSpan<float, Vector3> PositionKeyFrames = new(positionTimes, positions);
        public readonly UnsafeZippedSpan<float, Quaternion> RotationKeyFrames = new(rotationTimes, rotations);
        public readonly int Length = length;
    }
}