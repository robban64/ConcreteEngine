using System.Runtime.CompilerServices;
using AssimpScene = Silk.NET.Assimp.Scene;
using AssimpNode = Silk.NET.Assimp.Node;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using AssimpAnimation = Silk.NET.Assimp.Animation;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal sealed unsafe partial class ModelImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ModelAnimation? MakeAnimation(AssimpScene* scene)
    {
        if (!HasAnimationChannels(scene) || _boneIndexByName.Count == 0)
            return null;

        var animationCount = _sceneMeta.AnimationCount;
        var rootNode = scene->MRootNode->MTransformation;
        var boneMap = new Dictionary<string, int>(_boneIndexByName);
        return new ModelAnimation(animationCount, boneMap, in rootNode);
        
        static bool HasAnimationChannels(AssimpScene* scene)
        {
            for (uint i = 0; i < scene->MNumAnimations; i++)
            {
                var anim = scene->MAnimations[i];
                if (anim->MNumChannels > 0) return true;
            }

            return false;
        }
    }
    
    private static void ProcessAnimation(AssimpAnimation* aiAnim,  ModelAnimation? animation, Dictionary<uint, int> boneMap)
    {
        ArgumentNullException.ThrowIfNull(animation);

        var name = aiAnim->MName.AsString;
        var duration = (float)aiAnim->MDuration;
        var ticksPerSecond = (float)(aiAnim->MTicksPerSecond != 0 ? aiAnim->MTicksPerSecond : 25.0f);

        var channels = (int)aiAnim->MNumChannels;

        var clip = new AnimationClip(name, animation.BoneCount, duration, ticksPerSecond);
        animation.Clips.Add(clip);

        for (uint c = 0; c < channels; c++)
        {
            var aiChannel = aiAnim->MChannels[c];
            if (!boneMap.TryGetValue(AssimpUtils.GetNameHash(aiChannel->MNodeName), out var boneIndex))
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