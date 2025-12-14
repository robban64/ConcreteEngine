using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Render;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Shared.Rendering;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx) : IEngineWorldController
{
    private readonly World _world = ctx.World;
    private readonly Camera3D _camera = ctx.World.Camera;
    private readonly WorldRenderParams _renderParams = ctx.World.WorldRenderParams;

    public void CommitCamera(EditorSlot<EditorCameraState> slot)
    {
        if (slot.Gen != _camera.Generation)
        {
            _camera.FillData(out slot.State);
            slot.Gen = _camera.Generation;
            return;
        }

        _camera.SetFromData(in slot.State);
    }


    public void FetchCamera(EditorSlot<EditorCameraState> slot)
    {
        if (slot.Gen == _camera.Generation) return;
        _camera.FillData(out slot.State);
        slot.Gen = _camera.Generation;
    }

    public void CommitWorldRenderParams(EditorSlot<WorldParamsData> slot)
    {
        if (slot.Gen != _renderParams.Generation)
        {
            _renderParams.FillData(out slot.State);
            slot.Gen = _renderParams.Generation;
            return;
        }

        _renderParams.SetFromData(in slot.State);
    }

    public void FetchWorldRenderParams(EditorSlot<WorldParamsData> slot)
    {
        if (slot.Gen == _renderParams.Generation) return;
        _renderParams.FillData(out slot.State);
        slot.Gen = _renderParams.Generation;
    }


    public List<EditorParticleResource> GetEditorEmitter()
    {
        var span = _world.Particles.EmitterSpan;
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
        var span = _world.GetAnimationTableImpl().ModelIdSpan;
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
                    TicksPerSecond = (float)c.TicksPerSecond,
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