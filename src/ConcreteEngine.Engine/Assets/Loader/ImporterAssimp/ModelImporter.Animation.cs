using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using AssimpAnimation = Silk.NET.Assimp.Animation;
using AssimpScene = Silk.NET.Assimp.Scene;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

internal sealed unsafe partial class ModelImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ModelAnimation? RegisterAnimation(AssimpScene* scene)
    {
        if (!HasAnimationChannels(scene) || _boneIndexByName.Count == 0)
            return null;

        var animationCount = _sceneMeta.AnimationCount;
        var boneMap = new Dictionary<string, int>(_boneIndexByName);

        return new ModelAnimation(animationCount, boneMap);

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

    private static void ProcessAnimation(AssimpAnimation* aiAnim, ModelAnimation? animation)
    {
        ArgumentNullException.ThrowIfNull(animation);

        var name = aiAnim->MName.AsString;
        var duration = (float)aiAnim->MDuration;
        var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);

        var clip = new AnimationClip(name, animation.BoneCount, duration, ticksPerSecond);
        animation.Clips.Add(clip);

        var channelLen = (int)aiAnim->MNumChannels;
        var channels = aiAnim->MChannels;
        for (var c = 0; c < channelLen; c++)
        {
            var aiChannel = channels[c];
            if (!TryGetBoneIndex(AssimpUtils.GetNameHash(aiChannel->MNodeName), out var boneIndex))
                continue;

            // Position
            var posKeys = aiChannel->MPositionKeys;
            var posCount = (int)aiChannel->MNumPositionKeys;

            var rotKeys = aiChannel->MRotationKeys;
            var rotCount = (int)aiChannel->MNumRotationKeys;

            var channel = new AnimationChannel(posCount, rotCount);

            for (var k = 0; k < posCount; k++)
            {
                channel.PositionTimes[k] = (float)posKeys[k].MTime;
                channel.Positions[k] = posKeys[k].MValue;
            }

            for (var k = 0; k < rotCount; k++)
            {
                channel.RotationTimes[k] = (float)rotKeys[k].MTime;
                channel.Rotations[k] = rotKeys[k].MValue.AsQuaternion;
            }

            clip.Channels[boneIndex] = channel;
        }
    }
}