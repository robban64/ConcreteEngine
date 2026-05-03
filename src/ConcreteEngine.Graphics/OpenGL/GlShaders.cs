using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Handles;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaders : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendResourceStore<GlHandle> _shaderStore;

    private GlHandle _activeProg;

    internal GlShaders(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _shaderStore = ctx.Store.ShaderStore;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(GfxHandle shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef);
        if (_activeProg == handle) return;

        _activeProg = handle;
        _gl.UseProgram(handle.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnbindShader()
    {
        if (_activeProg == default) return;
        _activeProg = default;
        _gl.UseProgram(0);
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
            if (vertexShader > 0) _gl.DeleteShader(vertexShader);
            if (fragmentShader > 0) _gl.DeleteShader(fragmentShader);
            throw;
        }

        GlHandle handle = default;
        try
        {
            handle = CreateShaderProgram(vertexShader, fragmentShader);
        }
        catch
        {
            _gl.DeleteProgram(handle);
            throw;
        }
        finally
        {
            _gl.DetachShader(handle, vertexShader);
            _gl.DetachShader(handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        return _shaderStore.Add(new GlHandle(handle));
    }

    public int GetSamplersFromProgram(GfxHandle shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef).Value;

        UseShader(shaderRef);
        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out var uniformsLength);
        var samplers = 0;
        for (uint idx = 0; idx < uniformsLength; idx++)
        {
            _gl.GetActiveUniform(handle, idx, out _, out var type);
            //var uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (IsSamplerUniform(type))
            {
                samplers++;
            }
        }

        return samplers;
    }

    private GlHandle CreateShaderProgram(uint vertexShader, uint fragmentShader)
    {
        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status != (int)GLEnum.True)
            throw GraphicsException.ShaderLinkFailed(program.ToString(), _gl.GetProgramInfoLog(program));

        return new GlHandle(program);
    }

    private unsafe uint CompileShader(ShaderType shaderType, NativeView<byte> source)
    {
        var shader = _gl.CreateShader(shaderType);

        byte** pptr = stackalloc byte*[1];
        pptr[0] = source.Ptr;
        int len = source.Length;
        _gl.ShaderSource(shader, 1, pptr, &len);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var vStatus);
        if (vStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderCompileFailed(nameof(shader), _gl.GetShaderInfoLog(shader));

        return shader;
    }

    public List<(string, int)> GetUniformsFromProgram(GfxHandle shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef).Value;

        UseShader(shaderRef);
        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out var uniformsLength);
        var uniforms = new List<(string, int)>(uniformsLength);
        for (int i = 0; i < uniformsLength; i++)
        {
            var uniformName = _gl.GetActiveUniform(handle, (uint)i, out _, out var type);
            var uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (IsSamplerUniform(type)) continue;
            if (uniformLocation >= 0)
            {
                uniforms.Add((uniformName, uniformLocation));
            }
        }

        return uniforms;
    }

    private static bool IsSamplerUniform(UniformType type) =>
        type is UniformType.Sampler2D or UniformType.SamplerCube
            or UniformType.IntSampler2D or UniformType.Sampler2DShadow or UniformType.Sampler2DMultisample;
}