using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawEntityIndex(int entity, int submitIndex)
{
    public readonly int Entity = entity;
    public readonly int SubmitIndex = submitIndex;
}