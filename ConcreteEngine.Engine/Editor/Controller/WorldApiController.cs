using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Shared.Visuals;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx) : IEngineWorldController
{
    private readonly World _world = ctx.World;
    private readonly Camera _camera = ctx.World.Camera;
    private readonly WorldVisual _worldVisual = ctx.World.WorldVisual;

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
        if (slot.Gen != _worldVisual.Generation)
        {
            _worldVisual.FillData(out slot.State);
            slot.Gen = _worldVisual.Generation;
            return;
        }

        _worldVisual.SetFromData(in slot.State);
    }

    public void FetchWorldRenderParams(EditorSlot<WorldParamsData> slot)
    {
        if (slot.Gen == _worldVisual.Generation) return;
        _worldVisual.FillData(out slot.State);
        slot.Gen = _worldVisual.Generation;
    }

    public List<EditorParticleResource> GetParticleEmitters()
    {
        var span = _world.Particles.EmitterSpan;
        List<EditorParticleResource> emitters = new(span.Length);
        foreach (var it in span)
        {
            emitters.Add(new EditorParticleResource
            {
                MeshId = new EditorId(it.Mesh, EditorItemType.Model),
                Id = new EditorId(it.EmitterHandle, EditorItemType.Particle),
                Name = it.EmitterName,
                Generation = 1
            });
        }

        return emitters;
    }

}