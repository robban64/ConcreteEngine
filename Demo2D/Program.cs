
#region

using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using Demo2D;
using Silk.NET.Windowing;

#endregion

var builder = new GameEngineBuilder()
    .RegisterScene<DemoScene>();


var options = WindowOptions.Default with { Title = "Demo2D Game Engine" };

var host = new EngineWindowHost(
    options: options,
    backend: GraphicsBackend.OpenGl
);

host.Run(builder);