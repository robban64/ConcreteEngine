using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ModelAnimation
{
    private static ushort _idCounter;
    public readonly Id16<ModelAnimation> AnimationId = new(++_idCounter);

    [Inspectable] public readonly int AnimationCount;
    [Inspectable] public readonly List<AnimationClip> Clips;
    [Inspectable] public readonly Dictionary<string, int> BoneMapping;

    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public int BoneCount => BoneMapping.Count;

    public ModelAnimation(int animationCount, Dictionary<string, int> boneMapping)
    {
        ArgumentOutOfRangeException.ThrowIfZero(boneMapping.Count);
        AnimationCount = animationCount;
        BoneMapping = boneMapping;
        
        ParentIndices = new byte[boneMapping.Count];
        BindPose = new Matrix4x4[boneMapping.Count];
        InverseBindPose = new Matrix4x4[boneMapping.Count];

        Clips = new List<AnimationClip>(animationCount);
    }
}

public sealed class AnimationClip
{
    public string Name;
    public float Duration;
    public float TicksPerSecond;

    public int Length => Channels.Length;

    public readonly Channel[] Channels;

    public AnimationClip(string name, int boneCount, float duration, float ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boneCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        Name = name;
        Channels = new Channel[boneCount];
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
    }

    public sealed class Channel
    {
        public readonly float[] PositionTimes = [];
        public readonly Vector3[] Positions = [];

        public readonly float[] RotationTimes = [];
        public readonly Quaternion[] Rotations = [];

        public readonly int MaxLength;

        public Channel(int positionLength, int rotationLength)
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
}

public sealed class Skeleton
{
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public int Length => ParentIndices.Length;

    public Skeleton(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ParentIndices = new byte[length];
        BindPose = new Matrix4x4[length];
        InverseBindPose = new Matrix4x4[length];
    }
}