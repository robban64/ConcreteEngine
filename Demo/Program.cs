// See https://aka.ms/new-console-template for more information

#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Definitions;
using Demo;
using Silk.NET.Windowing;

#endregion

var builder = new GameEngineBuilder()
    .WithGraphicsBackend(GraphicsBackend.OpenGL)
    .WithWindowOptions(() =>
    {
        var options = WindowOptions.Default;
        options.Title = "Demo Game Engine";
        return options;
    })
    .ConfigureAssetManager(new AssetManagerConfiguration())
    .RegisterScene<DemoScene>();

using (var gameEngine = builder.Build())
{
    gameEngine.Run();
}