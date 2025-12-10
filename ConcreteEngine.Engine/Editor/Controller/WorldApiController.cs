#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Shared.Rendering;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx)
{
    public long CameraGeneration => ctx.World.Camera.Generation;
    public long WorldParamGeneration => ctx.World.WorldRenderParams.Version;

    private World World => ctx.World;

    public void ApplyCameraState(in CameraDataState data) => World.Camera.SetFromData(in data);

    public void WriteCameraState(out CameraDataState data) => World.Camera.FillData(out data);

    public void ApplyWorldRenderParams(in WorldParamsData data) => World.WorldRenderParams.SetFromData(in data);

    public void WriteWorldRenderParams(out WorldParamsData data) => World.WorldRenderParams.FillData(out data);

    public void WriteEmitterState(ref ParticleDataState data)
    {
        var emitter = World.Particles.GetEmitter(data.EmitterHandle);
        data.EmitterHandle = emitter.EmitterHandle;
        data.Definition = emitter.Definition;
        data.EmitterState = emitter.State;
    }
    
    public void ApplyEmitterState(in ParticleDataState data)
    {
        var emitter = World.Particles.GetEmitter(data.EmitterHandle);
        emitter.Definition = data.Definition;
        emitter.State = data.EmitterState;
    }

    public List<EditorParticleResource> GetEditorEmitter()
    {
        var span = World.Particles.EmitterSpan;
        List<EditorParticleResource> emitters = new(span.Length);
        foreach (var it in span)
        {
            emitters.Add(new EditorParticleResource
            {
                MeshId = new EditorId(it.MeshId, EditorItemType.Model),
                Id = new EditorId(it.EmitterHandle, EditorItemType.Particle),
                Name = it.EmitterName
            });
        }

        return emitters;
    }

    public List<EditorAnimationResource> GetEditorAnimations()
    {
        var span = World.GetAnimationTableImpl().ModelIdSpan;
        List<EditorAnimationResource> list = new(span.Length);
        ctx.AssetSystem.StoreImpl.ExtractList<Model, EditorAnimationResource>(list, static (it) =>
        {
            if (it.AnimationId <= 0) return null!;
            var span = it.Animation!.ClipDataSpan;
            var clips = new EditorAnimationClip[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                clips[i] = new EditorAnimationClip
                {
                    DisplayName = c.Name,
                    Duration = c.Duration,
                    TicksPerSecond = c.TicksPerSecond,
                    TrackCount = c.Tracks.Count
                };
            }

            return new EditorAnimationResource
            {
                Name = it.Name,
                Id = new EditorId(it.AnimationId, EditorItemType.Animation),
                ModelId = new EditorId(it.ModelId, EditorItemType.Model),
                Clips = clips
            };
        });


        return list;
    }
}