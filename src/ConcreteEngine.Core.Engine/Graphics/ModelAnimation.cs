using System.Numerics;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Renderer;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ModelAnimation
{
    public AnimationId AnimationId { get; internal set; }

    [Inspectable] public readonly int AnimationCount;
    [Inspectable] public readonly List<AnimationClip> Clips;
    [Inspectable] public readonly Dictionary<string, int> BoneMapping;

    public readonly Skeleton Skeleton;

    public int BoneCount => BoneMapping.Count;

    public ModelAnimation(int animationCount, Dictionary<string, int> boneMapping)
    {
        AnimationCount = animationCount;

        BoneMapping = boneMapping;
        Skeleton = new Skeleton(boneMapping.Count);
        Clips = new List<AnimationClip>(animationCount);

        Array.Fill(Skeleton.ParentIndices, -1);
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

public sealed class Skeleton
{
    public readonly int[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public int Length => ParentIndices.Length;

    public Skeleton(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ParentIndices = new int[length];
        BindPose = new Matrix4x4[length];
        InverseBindPose = new Matrix4x4[length];
    }
}

public sealed class AnimationChannel
{
    public readonly float[] PositionTimes = [];
    public readonly Vector3[] Positions = [];

    public readonly float[] RotationTimes = [];
    public readonly Quaternion[] Rotations = [];

    public readonly int MaxLength;

    public AnimationChannel(int positionLength, int rotationLength)
    {
        if (positionLength == 0 && rotationLength == 0)
            return;

        PositionTimes = new float[positionLength];
        RotationTimes = new float[rotationLength];

        Positions = new Vector3[positionLength];
        Rotations = new Quaternion[rotationLength];
        MaxLength = int.Max(positionLength, rotationLength);
    }
}