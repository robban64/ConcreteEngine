using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class WorldApiController(ApiContext ctx) : WorldController
{
    private readonly World _world = ctx.World;
    private readonly Camera _camera = ctx.World.Camera;
    private readonly WorldVisual _worldVisual = ctx.World.WorldVisual;

    public override void CommitCamera(SlotState<EditorCameraState> slot)
    {
        if (slot.Generation != _camera.Generation)
        {
            _camera.FillData(ref slot.Data);
            slot.Generation = _camera.Generation;
            return;
        }
        _camera.SetFromData(in slot.Data);
    }

    public override void FetchCamera(SlotState<EditorCameraState> slot)
    {
        if (slot.Generation == _camera.Generation) return;
        _camera.FillData(ref slot.Data);
        slot.Generation = _camera.Generation;
    }


    public override void CommitVisualParams(SlotState<EditorVisualState> slot)
    {
        if (slot.Generation != _worldVisual.Generation)
        {
            _worldVisual.FillData(out slot.Data);
            slot.Generation = _worldVisual.Generation;
            return;
        }

        _worldVisual.SetFromData(in slot.Data);
    }

    public override void FetchVisualParams(SlotState<EditorVisualState> slot)
    {
        if (slot.Generation == _worldVisual.Generation) return;
        _worldVisual.FillData(out slot.Data);
        slot.Generation = _worldVisual.Generation;
    }
/*
    public List<EditorParticleResource> GetParticleEmitters()
    {
        var span = _world.Particles.EmitterSpan;
        List<EditorParticleResource> emitters = new(span.Length);
        foreach (var it in span)
        {
            emitters.Add(new EditorParticleResource
            {
                MeshId = it.Mesh,
                Id = it.EmitterHandle,
                Name = it.EmitterName,
                Generation = 1
            });
        }

        return emitters;
    }*/
}