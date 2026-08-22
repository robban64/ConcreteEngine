using ConcreteEngine.Core.Engine.Graphics.Particles;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.App.Inspectors;

[EditorInspector(typeof(ParticleEmitter))]
internal partial class ParticleInspector : Inspector<ParticleEmitter>
{
    public override uint Icon => IconNames.Sparkles;
    public override InspectorId Id => InspectorId.Asset;

    public ParticleInspector()
    {
        Sections = _fields.CreateSections();
        ApplySectionLowFetchRate();
    }
}