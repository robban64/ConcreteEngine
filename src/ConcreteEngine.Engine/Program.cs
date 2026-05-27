using ConcreteEngine.Engine;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Demo;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.Windowing;

var builder = new GameEngineBuilder()
    .RegisterScene<Demo3DScene>();

var host = new EngineHost(
    options: WindowOptions.Default with { Title = "Demo 3D Game Engine" },
    backend: GraphicsBackend.OpenGl
);

host.Run(builder);