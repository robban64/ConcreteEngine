using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

//
public sealed class ModelData(int meshCount)
{
    public int TotalVertexCount;
    public int TotalFaceCount;

    public BoundingBox ModelBounds;
    public readonly MeshEntry[] Meshes = new MeshEntry[meshCount];
    public readonly Matrix4x4[] WorldTransforms = new Matrix4x4[meshCount];

    //public readonly Matrix4x4[] LocalTransforms = new Matrix4x4[meshCount];
}

public sealed class MeshEntry
{
    public readonly string Name;
    public MeshId MeshId;
    public MeshInfo Info;
    public BoundingBox LocalBounds;

    public MeshEntry(string name, MeshInfo info)
    {
        Name = name;
        Info = info;
    }
}

public sealed class ModelAnimation
{
    public readonly int AnimationCount;

    public readonly SkeletonData SkeletonData;
    
    public readonly Dictionary<string, int> BoneMapping;
    public readonly List<AnimationClip> Clips;

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