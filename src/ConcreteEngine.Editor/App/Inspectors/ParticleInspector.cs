using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ParticleEmitter))]
internal static partial class ParticleInspector
{
    private static ParticleEmitter Target => SelectionManager.Instance.SelectedSceneObject?.InspectParticle?.Emitter;
}