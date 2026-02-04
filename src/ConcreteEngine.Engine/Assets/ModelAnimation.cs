using System.Numerics;

namespace ConcreteEngine.Engine.Assets;


public sealed class ModelAnimation
{
    public readonly int AnimationCount;

    public readonly SkeletonData SkeletonData;
    public readonly List<AnimationClip> Clips;

    public readonly Dictionary<string, int> BoneMapping;

    public int BoneCount => BoneMapping.Count;

    internal ModelAnimation(int animationCount, Dictionary<string, int> boneMapping, in Matrix4x4 inverseRoot)
    {
        AnimationCount = animationCount;

        BoneMapping = boneMapping;
        SkeletonData = new SkeletonData(boneMapping.Count, in inverseRoot);
        Clips = new List<AnimationClip>(animationCount);
        
        Array.Fill(SkeletonData.ParentIndices, -1);

    }
}

public sealed class AnimationClip
{
    public string Name;
    public float Duration;
    public float TicksPerSecond;

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
    public readonly Matrix4x4 InverseRoot;
    public readonly int[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public SkeletonData(int length, in Matrix4x4 inverseRoot)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        InverseRoot = inverseRoot;
        ParentIndices = new int[length];
        BindPose = new Matrix4x4[length];
        InverseBindPose = new Matrix4x4[length];
    }
}

public readonly struct AnimationChannel(int positionLength, int rotationLength)
{
    public readonly float[] PositionTimes = new float[positionLength];
    public readonly Vector3[] Positions = new Vector3[positionLength];

    public readonly float[] RotationTimes = new float[rotationLength];
    public readonly Quaternion[] Rotations = new Quaternion[rotationLength];

    public readonly int MaxLength = int.Max(positionLength, rotationLength);
}