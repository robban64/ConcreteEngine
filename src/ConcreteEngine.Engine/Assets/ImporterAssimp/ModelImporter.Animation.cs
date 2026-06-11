using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Graphics;
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

    private static void ProcessAnimation(int index, AssimpAnimation* aiAnim, ModelRig? animation)
    {
        ArgumentNullException.ThrowIfNull(animation);

        var name = aiAnim->MName.AsString;
        var duration = (float)aiAnim->MDuration;
        var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);
        
        var clip = new AnimationClip(name, animation.BoneCount, duration, ticksPerSecond);
        var clipTrack = new BoneTrack[animation.BoneCount];

        var channels = aiAnim->MChannels;
        var channelLength = (int)aiAnim->MNumChannels;
        for (var c = 0; c < channelLength; c++)
        {
            var aiChannel = channels[c];
            if (!TryGetBoneIndex(AssimpUtils.GetNameHash(aiChannel->MNodeName), out var boneIndex))
                continue;

            // Position
            var posKeys = aiChannel->MPositionKeys;
            var posCount = (int)aiChannel->MNumPositionKeys;

            var rotKeys = aiChannel->MRotationKeys;
            var rotCount = (int)aiChannel->MNumRotationKeys;

            if (posCount == 0 && rotCount == 0)
            {
                clipTrack[boneIndex] = new BoneTrack();
                continue;
            }

            var track = new BoneTrack(posCount, rotCount);

            for (var k = 0; k < posCount; k++)
            {
                track.PositionTimes[k] = (float)posKeys[k].MTime;
                track.Positions[k] = posKeys[k].MValue;
            }

            for (var k = 0; k < rotCount; k++)
            {
                track.RotationTimes[k] = (float)rotKeys[k].MTime;
                track.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
            }

            clipTrack[boneIndex] = track;
        }
        
        animation.Clips[index] = clip;
        animation.ClipTracks[index] = clipTrack;
    }
}