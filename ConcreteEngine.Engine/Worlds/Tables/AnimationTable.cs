using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;

namespace ConcreteEngine.Engine.Worlds.Tables;

public unsafe struct AnimationClipData
{
    public float Duration;
    public float TicksPerSecond;
    public fixed ushort BoneTrack[64];
}


internal sealed class AnimationTable
{
    private const int DefaultClipCap = 64;
    private const int DefaultBoneBufferCap = 64 * DefaultClipCap;

    private int _idx = 0;
    private AnimationId MakeId() => new(++_idx);
    
    private int[] _animationByModel = new int[DefaultClipCap];
    private Matrix4x4[] _modelBoneInvTransform = new Matrix4x4[DefaultClipCap];
    private RangeU16[] _modelBoneRanges = new RangeU16[DefaultClipCap];
    private Matrix4x4[] _boneTransforms = new Matrix4x4[DefaultBoneBufferCap];
    private Matrix4x4[] _nodeTransforms = new Matrix4x4[DefaultBoneBufferCap];

    private RangeU16[] _clipRanges = new RangeU16[DefaultClipCap];
    private AnimationClip[] _clips = new AnimationClip[DefaultClipCap];

   // private RangeU16[] _clipTrackRanges = new RangeU16[DefaultClipCap];
    //private BoneClipTrack[] _clipBoneTrack = new BoneClipTrack[DefaultBoneBufferCap];

    public int TotalBones { get; private set; }
    public int TotalClips { get; private set; }
    
    public ModelAnimationView GetModelAnimationView(AnimationId animation)
    {
        if((uint)animation.Value > _animationByModel.Length)
            throw new ArgumentOutOfRangeException(nameof(animation));
        
        var slot = _animationByModel[animation - 1];
        if ((uint)slot > (uint)_modelBoneRanges.Length || (uint)slot > (uint)_modelBoneRanges.Length)
            throw new IndexOutOfRangeException();

        var boneRange = _modelBoneRanges[slot];
        var clipRange = _clipRanges[slot];
        if ((uint)(boneRange.Offset + boneRange.Length) > (uint)_boneTransforms.Length ||
            (uint)(clipRange.Offset + clipRange.Length) > (uint)_clips.Length)
        {
            throw new IndexOutOfRangeException();
        }

        var boneTransforms = _boneTransforms.AsSpan(boneRange.Offset, boneRange.Length);
        var nodes = _nodeTransforms.AsSpan(boneRange.Offset, boneRange.Length);
        var clips = _clips.AsSpan(clipRange.Offset, clipRange.Length);
        return new ModelAnimationView(clips, boneTransforms, nodes, ref _modelBoneInvTransform[slot], boneRange);
    }

    
    internal void Setup(AssetSystem assets)
    {
        _idx = 0;
        
        var models = new List<Model>(8);
        assets.StoreImpl.ExtractList<Model, Model>(models, static (it) => it.Animation != null ? it : null!);
        models.Sort();
        
        int totalBones = 0,  totalClips = 0, modelHighId = -1;
        foreach (var model in models)
        {
            totalBones += model.Animation!.BoneCount;
            totalClips += model.Animation.ClipDataSpan.Length;
            modelHighId = int.Max(modelHighId, model.ModelId);
        }
        
        TotalBones = totalBones;
        TotalClips = totalClips;
        if(TotalBones == 0 || TotalClips == 0) return;
        
        EnsureAnimatedCapacity(TotalBones, TotalClips);

        int boneOffset = 0;
        int clipTrackOffset = 0;
        for (var i = 0; i < models.Count; i++)
        {
            var animationId = MakeId();

            var model = models[i];
            var animation = model.Animation!;
            var modelBones = animation.BoneTransforms;
            var tableBones = _boneTransforms.AsSpan(boneOffset, modelBones.Length);
            var tableNodes = _nodeTransforms.AsSpan(boneOffset, modelBones.Length);

            _animationByModel[i] = model.ModelId;
            _modelBoneInvTransform[i] = model.Animation!.InverseRootTransform;
            _modelBoneRanges[i] = new RangeU16(boneOffset, modelBones.Length);
            modelBones.CopyTo(tableBones);
            animation.NodeTransforms.CopyTo(tableNodes);

            boneOffset += modelBones.Length;
            
            var animationClip = model.Animation!.ClipDataSpan;
            var tableClips = _clips.AsSpan(clipTrackOffset, animationClip.Length);
            _clipRanges[i] = new RangeU16(clipTrackOffset, animationClip.Length);
            animationClip.CopyTo(tableClips);
            
            clipTrackOffset += animationClip.Length;
            
            model.AttachAnimation(animationId);

            /*
            var animationClip = model.Animation!.ClipDataSpan;
            var tableClip = _clipBoneTrack.AsSpan(clipTrackOffset, animationClip.Length);
            _clipRanges[i] = new RangeU16(clipTrackOffset, animationClip.Length);
            clipTrackOffset += animationClip.Length;

            int localClipIdx = 0;
            foreach (var clip in animationClip)
            {
                _clipTrackRanges[i+localClipIdx] = new RangeU16()
                tableClip[localClipIdx++]
            }*/
        }

        void ProcessClips(AnimationClip clip)
        {
            foreach (var (key, value) in clip.BoneTracksMap)
            {
                    
            }

        }
    }
    

    private void EnsureAnimatedCapacity(int cap, int rangeCap)
    {
        if (_animationByModel.Length != _modelBoneInvTransform.Length ||
            _animationByModel.Length != _modelBoneRanges.Length)
            throw new InvalidOperationException("Mismatch size for model animation tables");

        if (_boneTransforms.Length < cap)
        {
            var newCap = Arrays.CapacityGrowthSafe(_boneTransforms.Length, cap);
            Array.Resize(ref _boneTransforms, newCap);
            Console.WriteLine("animation bones resize");
        }

        if (_modelBoneRanges.Length < rangeCap)
        {
            var newCap = Arrays.CapacityGrowthSafe(_modelBoneRanges.Length, rangeCap, Arrays.TableSmallThreshold);
            Array.Resize(ref _modelBoneRanges, newCap);
            Array.Resize(ref _nodeTransforms, newCap);

            Array.Resize(ref _animationByModel, newCap);
            Array.Resize(ref _modelBoneInvTransform, newCap);
            Console.WriteLine("animation clips resize");
        }
    }
    
}