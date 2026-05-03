using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.Demo;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Silk.NET.Windowing;

var builder = new GameEngineBuilder()
    .RegisterScene<Demo3DScene>();

var host = new EngineHost(
    options: WindowOptions.Default with { Title = "Demo 3D Game Engine" },
    backend: GraphicsBackend.OpenGl
);

host.Run(builder);