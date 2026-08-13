using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ParticleEmitter))]
internal partial class ParticleInspector : Inspector<ParticleInspector>
{
    private static ParticleEmitter Target =>
        SelectionManager.Instance.SelectedSceneObject?.GetInstance<ParticleInstance>().Emitter!;

    public unsafe void Draw()
    {
        AppDraw.Section("Emitter"u8, &DrawEmitterParams);
        AppDraw.Section("Particle"u8, &DrawParticleParams);
    }
}