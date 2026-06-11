using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics;


internal sealed class AnimationRig
{
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    public readonly AnimationChannel[][] Channels;

    public readonly Id16<ModelAnimation> AnimationId;

    public AnimationRig(ModelAnimation source, AnimationChannel[][] channels)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentOutOfRangeException.ThrowIfZero(source.AnimationId.Value, nameof(source.AnimationId));

        AnimationId = source.AnimationId;

        ParentIndices = source.ParentIndices;
        BindPose = source.BindPose;
        InverseBindPose = source.InverseBindPose;
        Channels = channels;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SkinningContext GetSkinningContext(int clip)
    {
        if ((uint)clip > (uint)Channels.Length)
            Throwers.IndexOutOfRange(nameof(AnimationChannel), clip, Channels.Length);

        return new SkinningContext(ParentIndices, BindPose, InverseBindPose, Channels[clip]);
    }
}

public sealed class ModelAnimation
{
    private static ushort _idCounter;
    public readonly Id16<ModelAnimation> AnimationId = new(++_idCounter);

    public readonly int AnimationCount;
    public readonly AnimationClip[] Clips;
    public readonly Dictionary<string, int> BoneMapping;

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

        Clips = new AnimationClip[animationCount];
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