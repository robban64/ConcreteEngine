using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class ModelRig : IDisposable
{
    private static ushort _idCounter;
    
    public readonly Id16<ModelRig> Id = new(++_idCounter);
    public readonly int ClipCount;
    public readonly int BoneCount;

    public readonly AnimationClip[] Clips;
    public readonly Dictionary<string, int> BoneMapping; 
    
    public readonly byte[] ParentIndices;
    public readonly Matrix4x4[] BindPose;
    public readonly Matrix4x4[] InverseBindPose;
    
    private NativeArray<byte> _clipsBuffer;
    private NativeView<NativeClip> _clipsView;

    public ModelRig(int animationCount, Dictionary<string, int> boneMapping)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(animationCount);
        ArgumentNullException.ThrowIfNull(boneMapping);
        ArgumentOutOfRangeException.ThrowIfZero(boneMapping.Count);
        
        BoneMapping = boneMapping;
        ParentIndices = new byte[boneMapping.Count];
        BindPose = new Matrix4x4[boneMapping.Count];
        InverseBindPose = new Matrix4x4[boneMapping.Count];

        Clips = new AnimationClip[animationCount];
        
        ClipCount = animationCount;
        BoneCount = boneMapping.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SkinningContext GetSkinningContext(int clip)
    {
        if(_clipsView.IsNull || (uint)clip >= (uint)ClipCount)
            Throwers.InvalidOperation(nameof(_clipsBuffer));
        
        return new SkinningContext(ParentIndices, BindPose, InverseBindPose, _clipsView[clip]);
    }
    
    internal unsafe void SetClipBuffer(NativeArray<byte> buffer)
    {
        if(!_clipsBuffer.IsNull) Throwers.InvalidOperation("Clip buffer already set");
        
        if(buffer.IsNull) Throwers.NullPointer(nameof(buffer));
        if(buffer.Length == 0) Throwers.InvalidArgument(nameof(buffer), "is empty");

        _clipsBuffer = buffer;
        var view = _clipsView = new NativeView<NativeClip>((NativeClip*)_clipsBuffer.Ptr, ClipCount);
        for (var i = 0; i < ClipCount; i++)
        {
            if(view + i == null) Throwers.NullPointer(nameof(buffer));
            var clip = view[i];
            if(clip.IsNull || clip.BoneTracks.IsNull) Throwers.NullPointer(nameof(buffer));
            for (var j = 0; j < clip.Length; j++)
            {
                if(clip.BoneTracks + j == null) Throwers.NullPointer(nameof(buffer));
            }
        }
    }

    public void Dispose()
    {
        _clipsView = NativeView<NativeClip>.MakeNull();
        _clipsBuffer.Dispose();
    }
}

public sealed class AnimationClip
{
    public readonly string Name;
    public readonly float Duration;
    public readonly float TicksPerSecond;

    public readonly int ActiveChannelCount;

    public AnimationClip(string name, int activeChannelCount, float duration, float ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(activeChannelCount);
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksPerSecond);

        Name = name;
        ActiveChannelCount = activeChannelCount;
        Duration = duration;
        TicksPerSecond = ticksPerSecond;
    }
}
