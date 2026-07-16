using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;


[EditorInspector(typeof(ParticleEmitter))]
internal partial class ParticleInspector : Inspector<ParticleInspector>
{
    private static ParticleEmitter Target =>
        SelectionManager.Instance.SelectedSceneObject?.GetInstance<ParticleInstance>()?.Emitter;

    public unsafe void Draw()
    {
        AppDraw.Section("Emitter"u8, &DrawEmitterParams);
        AppDraw.Section("Particle"u8, &DrawParticleParams);
    }
}