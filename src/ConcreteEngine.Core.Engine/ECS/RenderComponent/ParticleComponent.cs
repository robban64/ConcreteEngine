using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.RenderComponent;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleComponent(Id16<ParticleEmitter> emitterId)
    : IRenderComponent<ParticleComponent>
{
    public Id16<ParticleEmitter> EmitterId = emitterId;
}