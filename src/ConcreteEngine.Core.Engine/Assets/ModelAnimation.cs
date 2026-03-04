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

public readonly struct SkeletonData
{
    public readonly int[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    
    public int Length => ParentIndices.Length;

    public SkeletonData(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ParentIndices = new int[length];
        BindPose = new Matrix4x4[length];
        InverseBindPose = new Matrix4x4[length];
    }
}

public readonly struct AnimationChannel
{
    public readonly float[] PositionTimes;
    public readonly Vector3[] Positions;

    public readonly float[] RotationTimes;
    public readonly Quaternion[] Rotations;

    public readonly int MaxLength;

    public AnimationChannel(int positionLength, int rotationLength)
    {
        PositionTimes = new float[positionLength];
        RotationTimes = new float[rotationLength];

        Positions = new Vector3[positionLength];
        Rotations = new Quaternion[rotationLength];
        MaxLength = int.Max(positionLength, rotationLength);
    }
}