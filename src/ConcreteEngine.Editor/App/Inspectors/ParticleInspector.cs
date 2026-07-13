using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;


[EditorInspector(typeof(ParticleEmitter))]
internal partial class ParticleInspector : Inspector<ParticleInspector>
{
    private static ParticleEmitter Target => SelectionManager.Instance.SelectedSceneObject?.InspectParticle?.Emitter;
}