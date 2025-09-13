using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaderFactory() : GlFactory()
{

    public unsafe GlUboHandle CreateUniformBuffer(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity, uint blockSize,
        out UniformBufferMeta meta) 
    {
        
        meta = new UniformBufferMeta(slot, blockSize);

        nuint capacity = UniformBufferUtils.GetDefaultCapacity(meta.Stride, defaultCapacity);
        var handle = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.UniformBuffer, handle);
        Gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.StaticDraw);
        Gl.BindBufferBase(BufferTargetARB.UniformBuffer, meta.BindingIdx, handle);
        Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        return new GlUboHandle(handle);
    }
    

    public GlShaderHandle CreateShader(string vertexSource, string fragmentSource,
        out ShaderLayout shaderLayout, out ShaderMeta meta)
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        uint handle = CreateShaderProgram(vertexShader, fragmentShader);
        Gl.DetachShader(handle, vertexShader);
        Gl.DetachShader(handle, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);


        Gl.UseProgram(handle);
        GetUniformsFromProgram(handle, out var uniformPair, out var samplers);
        shaderLayout = new ShaderLayout(uniformPair);
        Gl.UseProgram(0);

        meta = new ShaderMeta((uint)samplers);
        return new GlShaderHandle(handle);
    }


    private uint CreateShaderProgram(uint vertexShader, uint fragmentShader)
    {
        uint program = Gl.CreateProgram();
        Gl.AttachShader(program, vertexShader);
        Gl.AttachShader(program, fragmentShader);
        Gl.LinkProgram(program);

        Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderLinkFailed(program.ToString(), Gl.GetProgramInfoLog(program));

        return program;
    }

    private uint CreateShader(ShaderType shaderType, string source)
    {
        uint shader = Gl.CreateShader(shaderType);
        Gl.ShaderSource(shader, source);
        Gl.CompileShader(shader);

        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderCompileFailed(nameof(shaderType), Gl.GetShaderInfoLog(shader));

        return shader;
    }

    private void GetUniformsFromProgram(uint handle, out List<(string, int)> uniforms, out int samplers)
    {
        Gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        uniforms = new List<(string, int)>(uniformsLength);
        samplers = 0;
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = Gl.GetActiveUniform(handle, uniformIndex, out _, out var type);
            int uniformLocation = Gl.GetUniformLocation(handle, uniformName);
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