#region

using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Editor.Data;

public struct EditorParticleState
{
    public ParticleDefinition Definition;
    public ParticleEmitterState EmitterState;
    public int EmitterHandle;
}