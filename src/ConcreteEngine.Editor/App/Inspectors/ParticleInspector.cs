using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ParticleEmitter))]
internal partial class ParticleInspector : Inspector<ParticleInspector>
{
    private static ParticleEmitter Target =>
        SelectionManager.Instance.SelectedSceneObject?.GetInstance<ParticleInstance>().Emitter!;

    public override uint Icon => IconNames.Sparkles;

    public override void Draw()
    {
        foreach (var section in Sections) section.Draw();
    }
}