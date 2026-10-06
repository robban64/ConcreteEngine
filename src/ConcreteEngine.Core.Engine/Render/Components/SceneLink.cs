using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Engine.Render.Components;

public readonly struct SceneLink(SceneObjectId sceneId) : IRenderComponent<SceneLink>
{
    public readonly SceneObjectId SceneId = sceneId;

    public bool IsLinked
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => SceneId != default;
    }
}