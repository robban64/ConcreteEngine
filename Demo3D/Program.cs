using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using Demo3D;
using Silk.NET.Windowing;

var builder = new GameEngineBuilder()
    .ConfigureAssetManager(new AssetManagerConfiguration())
    .RegisterScene<Demo3DScene>();


var options = WindowOptions.Default with { Title = "Demo 3D Game Engine" };


var host = new EngineWindowHost(
    options: options,
    backend: GraphicsBackend.OpenGL
);

host.Run(builder);