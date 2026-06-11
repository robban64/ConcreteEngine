using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Graphics;
using Silk.NET.Assimp;
using AssimpAnimation = Silk.NET.Assimp.Animation;
using AssimpScene = Silk.NET.Assimp.Scene;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal sealed unsafe partial class ModelImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ModelRig? RegisterAnimation(AssimpScene* scene)
    {
        if (!HasAnimationChannels(scene) || _boneIndexByName.Count == 0)
            return null;

        var animationCount = _sceneMeta.AnimationCount;
        var boneMap = new Dictionary<string, int>(_boneIndexByName);

        return new ModelRig(animationCount, boneMap);

        static bool HasAnimationChannels(AssimpScene* scene)
        {
            var len = scene->MNumAnimations;
            for (uint i = 0; i < len; i++)
            {
                var anim = scene->MAnimations[i];
                if (anim->MNumChannels > 0) return true;
            }

            return false;
        }
    }
    
    private static NativeArray<byte> AllocateAnimation(
        AssimpAnimation** mAnimations,
        AnimationClip[] animationClips,
        int animationCount,
        int boneCount)
    {
        int totalArenaBytes = animationCount * Unsafe.SizeOf<NativeClip>();

        for (int i = 0; i < animationCount; i++)
        {
            var aiAnim = mAnimations[i];
        
            totalArenaBytes += boneCount * Unsafe.SizeOf<NativeBoneTrack>();

            var channelLength = (int)aiAnim->MNumChannels;
            for (var c = 0; c < channelLength; c++)
            {
                var aiChannel = aiAnim->MChannels[c];
                if (!TryGetBoneIndex(AssimpUtils.GetNameHash(aiChannel->MNodeName), out _)) continue;
            
                var posCount = (int)aiChannel->MNumPositionKeys;
                var rotCount = (int)aiChannel->MNumRotationKeys;
            
                totalArenaBytes += (posCount * sizeof(float)) +
                                   (rotCount * sizeof(float)) +
                                   (posCount * Unsafe.SizeOf<Vector3>()) +
                                   (rotCount * Unsafe.SizeOf<Quaternion>());
            }
        }

        NativeArray<byte> clipBuffer = NativeArray.Allocate<byte>(totalArenaBytes);
        
        int cursor = 0;
        NativeAllocator allocator = new NativeAllocator(clipBuffer, ref cursor);
        NativeView<NativeClip> clips = allocator.AllocSlice<NativeClip>(animationCount);
        
        for (int i = 0; i < animationCount; i++)
        {
            var aiAnim = mAnimations[i];

            NativeView<NativeBoneTrack> tracks = allocator.AllocSlice<NativeBoneTrack>(boneCount);
            clips[i] = new NativeClip(tracks);

            var channelLength = (int)aiAnim->MNumChannels;
            for (var c = 0; c < channelLength; c++)
            {
                var aiChannel = aiAnim->MChannels[c];
                if (!TryGetBoneIndex(AssimpUtils.GetNameHash(aiChannel->MNodeName), out var boneIndex))
                    continue;

                var posCount = (int)aiChannel->MNumPositionKeys;
                var rotCount = (int)aiChannel->MNumRotationKeys;

                int floatCount = posCount + rotCount + (posCount * 3) + (rotCount * 4);

                if (floatCount == 0)
                {
                    tracks[boneIndex] = new NativeBoneTrack(null, 0, 0);
                    continue;
                }

                var trackData = allocator.AllocSlice<float>(floatCount);
                var track = new NativeBoneTrack(trackData.Ptr, posCount, rotCount);
                WriteChannels(aiChannel, track);
                tracks[boneIndex] = track;
            }
            


            var name = aiAnim->MName.AsString;
            var duration = (float)aiAnim->MDuration;
            var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);
            animationClips[i] = new AnimationClip(name, channelLength, duration, ticksPerSecond);
        }

        return clipBuffer;
    }
/*

    private static void AllocateAnimation(
        AssimpAnimation** mAnimations,
        AnimationClip[] animationClips,
        NativeAllocator allocator,
        int animationCount,
        int boneCount)
    {
        NativeView<NativeClip> clips = allocator.AllocSlice<NativeClip>(animationCount);
        
        for (int i = 0; i < animationCount; i++)
        {
            var aiAnim = mAnimations[i];

            NativeView<NativeBoneTrack> tracks = allocator.AllocSlice<NativeBoneTrack>(boneCount);
            clips[i] = new NativeClip(tracks);

            var channelLength = (int)aiAnim->MNumChannels;
            for (var c = 0; c < channelLength; c++)
            {
                var aiChannel = aiAnim->MChannels[c];
                if (!TryGetBoneIndex(AssimpUtils.GetNameHash(aiChannel->MNodeName), out var boneIndex))
                    continue;

                var posCount = (int)aiChannel->MNumPositionKeys;
                var rotCount = (int)aiChannel->MNumRotationKeys;

                int floatCount = posCount + rotCount + (posCount * 3) + (rotCount * 4);

                if (floatCount == 0)
                {
                    tracks[boneIndex] = new NativeBoneTrack(null, 0, 0);
                    continue;
                }

                var trackData = allocator.AllocSlice<float>(floatCount);
                var track = new NativeBoneTrack(trackData.Ptr, posCount, rotCount);
                WriteChannels(aiChannel, track);
                tracks[boneIndex] = track;
            }
            


            var name = aiAnim->MName.AsString;
            var duration = (float)aiAnim->MDuration;
            var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);
            animationClips[i] = new AnimationClip(name, channelLength, duration, ticksPerSecond);
        }
    }
*/
    private static void WriteChannels(NodeAnim* aiChannel, NativeBoneTrack track)
    {
        var posKeys = aiChannel->MPositionKeys;
        var rotKeys = aiChannel->MRotationKeys;

        for (var k = 0; k < track.PosCount; k++)
            track.PositionTimes[k] = (float)posKeys[k].MTime;
        for (var k = 0; k < track.RotCount; k++)
            track.RotationTimes[k] = (float)rotKeys[k].MTime;

        for (var k = 0; k < track.PosCount; k++)
            track.Positions[k] = posKeys[k].MValue;
        for (var k = 0; k < track.RotCount; k++)
            track.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
    }

    private static AnimationClip ProcessAnimation(AssimpAnimation* aiAnim, NativeView<NativeBoneTrack> tracks,
        int index, int boneCount)
    {
        ArgumentNullException.ThrowIfNull(aiAnim);


        var channels = aiAnim->MChannels;
        var channelLength = (int)aiAnim->MNumChannels;
        for (var c = 0; c < channelLength; c++)
        {
            var aiChannel = channels[c];
            if (!TryGetBoneIndex(AssimpUtils.GetNameHash(aiChannel->MNodeName), out var boneIndex))
                continue;

            var posCount = (int)aiChannel->MNumPositionKeys;
            var rotCount = (int)aiChannel->MNumRotationKeys;

            if (posCount == 0 && rotCount == 0)
            {
                tracks[boneIndex] = new NativeBoneTrack(null, 0, 0);
                continue;
            }

            ref var track = ref tracks[boneIndex];
            var posKeys = aiChannel->MPositionKeys;
            for (var k = 0; k < posCount; k++)
            {
                track.PositionTimes[k] = (float)posKeys[k].MTime;
                track.Positions[k] = posKeys[k].MValue;
            }

            var rotKeys = aiChannel->MRotationKeys;
            for (var k = 0; k < rotCount; k++)
            {
                track.RotationTimes[k] = (float)rotKeys[k].MTime;
                track.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
            }
        }

        var name = aiAnim->MName.AsString;
        var duration = (float)aiAnim->MDuration;
        var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);
        return new AnimationClip(name, boneCount, duration, ticksPerSecond);
    }
}