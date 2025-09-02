
#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Definitions;
using Demo;
using Silk.NET.Windowing;

#endregion

var builder = new GameEngineBuilder()
    .ConfigureAssetManager(new AssetManagerConfiguration())
    .RegisterScene<DemoScene>();


var options = WindowOptions.Default with { Title = "Demo Game Engine" };

var host = new EngineWindowHost(
    options: options,
    backend: GraphicsBackend.OpenGL
);

host.Run(builder);