using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaders 
{
    private static GL Gl => GlBackendDriver.Gl;

    private readonly BackendResourceStore<GlHandle> _shaderStore;

    internal GlShaders(GlCtx ctx)
    {
        _shaderStore = ctx.Store.ShaderStore;
    }
    

    public GfxHandle CreateShader(NativeView<byte> vertexSource, NativeView<byte> fragmentSource)
    {
        uint vertexShader = 0, fragmentShader = 0;

        try
        {
            vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
        }
        catch
        {
            if (vertexShader > 0) Gl.DeleteShader(vertexShader);
            if (fragmentShader > 0) Gl.DeleteShader(fragmentShader);
            throw;
        }

        GlHandle handle = default;
        try
        {
            handle = CreateShaderProgram(vertexShader, fragmentShader);
        }
        catch
        {
            Gl.DeleteProgram(handle);
            throw;
        }
        finally
        {
            Gl.DetachShader(handle, vertexShader);
            Gl.DetachShader(handle, fragmentShader);
            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);
        }

        return _shaderStore.Add(new GlHandle(handle));
    }

    public int GetSamplersFromProgram(GfxHandle shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef).Value;
        Gl.UseProgram(handle);

        Gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out var uniformsLength);
        var samplers = 0;
        for (uint idx = 0; idx < uniformsLength; idx++)
        {
            Gl.GetActiveUniform(handle, idx, out _, out var type);
            //var uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (IsSamplerUniform(type))
            {
                samplers++;
            }
        }

        Gl.UseProgram(0);
        return samplers;
    }

    private GlHandle CreateShaderProgram(uint vertexShader, uint fragmentShader)
    {
        var program = Gl.CreateProgram();
        Gl.AttachShader(program, vertexShader);
        Gl.AttachShader(program, fragmentShader);
        Gl.LinkProgram(program);

        Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status != (int)GLEnum.True)
            throw GraphicsException.ShaderLinkFailed(program.ToString(), Gl.GetProgramInfoLog(program));

        return new GlHandle(program);
    }

    private unsafe uint CompileShader(ShaderType shaderType, NativeView<byte> source)
    {
        var shader = Gl.CreateShader(shaderType);

        byte** pptr = stackalloc byte*[1];
        pptr[0] = source.Ptr;
        int len = source.Length;
        Gl.ShaderSource(shader, 1, pptr, &len);
        Gl.CompileShader(shader);

        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out var vStatus);
        if (vStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderCompileFailed(nameof(shader), Gl.GetShaderInfoLog(shader));

        return shader;
    }

    public List<(string, int)> GetUniformsFromProgram(GfxHandle shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef).Value;
        Gl.UseProgram(handle);
        Gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out var uniformsLength);
        var uniforms = new List<(string, int)>(uniformsLength);
        for (int i = 0; i < uniformsLength; i++)
        {
            var uniformName = Gl.GetActiveUniform(handle, (uint)i, out _, out var type);
            var uniformLocation = Gl.GetUniformLocation(handle, uniformName);
            if (IsSamplerUniform(type)) continue;
            if (uniformLocation >= 0)
            {
                uniforms.Add((uniformName, uniformLocation));
            }
        }
        Gl.UseProgram(0);
        return uniforms;
    }

    private static bool IsSamplerUniform(UniformType type) =>
        type is UniformType.Sampler2D or UniformType.SamplerCube
            or UniformType.IntSampler2D or UniformType.Sampler2DShadow or UniformType.Sampler2DMultisample;
}