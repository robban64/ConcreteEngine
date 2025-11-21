#region

using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Demo3D;
using Silk.NET.Windowing;

#endregion

var builder = new GameEngineBuilder()
    .RegisterScene<Demo3DScene>();


var options = WindowOptions.Default with { Title = "Demo 3D Game Engine" };


var host = new EngineWindowHost(
    options: options,
    backend: GraphicsBackend.OpenGl
);

host.Run(builder);