using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class EnvironmentProcessor
{
    public static void SubmitDrawSkybox(DrawCommandBuffer commandBuffer, Skybox sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.MeshId, sky.MaterialId);
        commandBuffer.Submit(cmd, meta, in DrawCommandBuffer.TransformIdentity);
    }

}