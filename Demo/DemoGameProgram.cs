#region

using ConcreteEngine.Core;

#endregion

namespace Demo;

public sealed class DemoGameProgram : GameProgram
{
    protected override void Load()
    {
        RegisterScene<DemoScene>();
    }

    protected override void Unload()
    {
    }

    protected override void Update(float deltaTime)
    {
    }

    protected override void SceneChanged(GameScene? previous, GameScene current)
    {
    }

}