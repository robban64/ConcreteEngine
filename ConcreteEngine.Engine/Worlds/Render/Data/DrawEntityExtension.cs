using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal static class DrawEntityExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawCommandId ToCommandId(this EntitySourceKind source)
    {
        return source switch
        {
            EntitySourceKind.Model => DrawCommandId.Model,
            EntitySourceKind.Particle => DrawCommandId.Particle,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}