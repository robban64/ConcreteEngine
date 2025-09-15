using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal static class GlUtils
{
    internal static void GetUniformsFromProgram(GL gl, uint handle, out List<(string, int)> uniforms, out int samplers)
    {
        gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        uniforms = new List<(string, int)>(uniformsLength);
        samplers = 0;
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = gl.GetActiveUniform(handle, uniformIndex, out _, out var type);
            int uniformLocation = gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add((uniformName, uniformLocation));
            }

            if (type == UniformType.Sampler2D ||
                type == UniformType.SamplerCube ||
                type == UniformType.IntSampler2D)
            {
                samplers++;
            }
        }
    }

}