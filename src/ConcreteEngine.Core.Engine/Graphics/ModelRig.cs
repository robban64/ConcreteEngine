using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ModelRig
{
    private static ushort _idCounter;
    public readonly Id16<ModelRig> Id = new(++_idCounter);

    public readonly AnimationClip[] Clips;
    public readonly Dictionary<string, int> BoneMapping; 
    
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    public readonly BoneTrack[][] ClipTracks;

    public int ClipCount => Clips.Length;
    public int BoneCount => BoneMapping.Count;

    public ModelRig(int animationCount, Dictionary<string, int> boneMapping)
    {
        ArgumentOutOfRangeException.ThrowIfZero(boneMapping.Count);
        BoneMapping = boneMapping;

        ParentIndices = new byte[boneMapping.Count];
        BindPose = new Matrix4x4[boneMapping.Count];
        InverseBindPose = new Matrix4x4[boneMapping.Count];

        Clips = new AnimationClip[animationCount];
        ClipTracks = new BoneTrack[animationCount][];
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SkinningContext GetSkinningContext(int clip)
    {
        if ((uint)clip > (uint)ClipTracks.Length)
            Throwers.IndexOutOfRange(nameof(BoneTrack), clip, ClipTracks.Length);

        return new SkinningContext(ParentIndices, BindPose, InverseBindPose, ClipTracks[clip]);
    }
}

public sealed class AnimationClip
{
    public readonly string Name;
    public readonly float Duration;
    public readonly float TicksPerSecond;

    public readonly int Length;


    public AnimationClip(string name, int boneCount, float duration, float ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(boneCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        Name = name;
        Length = boneCount;
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
    }
}
