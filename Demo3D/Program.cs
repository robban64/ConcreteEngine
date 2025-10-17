using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Demo3D;
using Silk.NET.Windowing;

var builder = new GameEngineBuilder()
    .RegisterScene<Demo3DScene>();


var options = WindowOptions.Default with { Title = "Demo 3D Game Engine" };


var host = new EngineWindowHost(
    options: options,
    backend: GraphicsBackend.OpenGl
);

host.Run(builder);