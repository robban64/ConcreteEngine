using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.ModelImporterV2;

//
internal sealed class ModelData(int meshCount)
{
    public readonly MeshEntry[] Meshes = new MeshEntry[meshCount];

    //public readonly Matrix4x4[] LocalTransforms = new Matrix4x4[meshCount];
    public readonly Matrix4x4[] WorldTransforms = new Matrix4x4[meshCount];
}

internal sealed class MeshEntry
{
    public required string Name;
    public MeshId MeshId;
    public MeshInfo Info;
    public BoundingBox LocalBounds;
}

internal readonly struct MeshInfo(int vertexCount, byte meshIndex, byte materialIndex, byte numBones)
{
    public readonly int VertexCount = vertexCount;
    public readonly byte MeshIndex = meshIndex;
    public readonly byte MaterialIndex = materialIndex;
    public readonly byte NumBones = numBones;
}

internal sealed class Animation
{
    public readonly int BoneCount;
    public readonly int AnimationCount;

    public readonly SkeletonData SkeletonData;
    public readonly Dictionary<string, int> BoneMapping;

    public readonly AnimationClip[] Clips;


    public Animation(int boneCount, int animationCount)
    {
        BoneCount = boneCount;
        AnimationCount = animationCount;

        BoneMapping = new Dictionary<string, int>(boneCount);
        SkeletonData = new SkeletonData(boneCount);

        Clips = new AnimationClip[animationCount];
    }
}

internal sealed class AnimationClip
{
    public string Name;
    public float Duration;
    public float TicksPerSecond;

    public readonly AnimationChannel[] Channels;

    public AnimationClip(string name, int channels, float duration, float ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        Name = name;
        Channels =  new AnimationChannel[channels];
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
    }
}

internal readonly struct SkeletonData
{
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;

    public SkeletonData(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        ParentIndices = new byte[length];
        BindPose = new Matrix4x4[length];
        InverseBindPose = new Matrix4x4[length];
    }
}

internal readonly struct AnimationChannel(int positionLength, int rotationLength)
{
    public readonly float[] PositionTimes = new float[positionLength];
    public readonly Vector3[] Positions = new Vector3[positionLength];

    public readonly float[] RotationTimes = new float[rotationLength];
    public readonly Quaternion[] Rotations = new Quaternion[rotationLength];

    public readonly int MaxLength = int.Max(positionLength, rotationLength);
}