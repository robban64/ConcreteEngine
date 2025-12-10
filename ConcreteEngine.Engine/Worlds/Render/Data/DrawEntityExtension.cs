#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

internal static class DrawEntityExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawCommandId ToCommandId(this RenderSourceKind source)
    {
        return source switch
        {
            RenderSourceKind.Model => DrawCommandId.Model,
            RenderSourceKind.Particle => DrawCommandId.Particle,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}