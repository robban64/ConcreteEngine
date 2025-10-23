#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaders : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendResourceStore<ShaderId, GlShaderHandle> _shaderStore;

    private GlShaderHandle _activeProg;

    internal GlShaders(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _shaderStore = ctx.Store.Shader;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UseShader(GfxRefToken<ShaderId> shaderRef)
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


    public GfxRefToken<ShaderId> CreateShader(string vertexSource, string fragmentSource)
    {
        var vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        var fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        var handle = CreateShaderProgram(vertexShader, fragmentShader);
        _gl.DetachShader(handle, vertexShader);
        _gl.DetachShader(handle, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        return _shaderStore.Add(new GlShaderHandle(handle));
    }

    public List<(string, int)> GetUniformsFromProgram(GfxRefToken<ShaderId> shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef).Value;

        UseShader(shaderRef);
        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        var uniforms = new List<(string, int)>(uniformsLength);
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = _gl.GetActiveUniform(handle, uniformIndex, out _, out var type);
            int uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add((uniformName, uniformLocation));
            }
        }

        return uniforms;
    }

    public int GetSamplersFromProgram(GfxRefToken<ShaderId> shaderRef)
    {
        var handle = _shaderStore.GetHandle(shaderRef).Value;

        UseShader(shaderRef);
        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        int samplers = 0;
        for (uint idx = 0; idx < uniformsLength; idx++)
        {
            string uniformName = _gl.GetActiveUniform(handle, idx, out _, out var type);
            int uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (type == UniformType.Sampler2D ||
                type == UniformType.SamplerCube ||
                type == UniformType.IntSampler2D)
            {
                samplers++;
            }
        }

        return samplers;
    }

    private uint CreateShaderProgram(uint vertexShader, uint fragmentShader)
    {
        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vertexShader);
        _gl.AttachShader(program, fragmentShader);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int lStatus);
        if (lStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderLinkFailed(program.ToString(), _gl.GetProgramInfoLog(program));

        return program;
    }

    private uint CreateShader(ShaderType shaderType, string source)
    {
        uint shader = _gl.CreateShader(shaderType);
        _gl.ShaderSource(shader, source);

        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int vStatus);
        if (vStatus != (int)GLEnum.True)
            throw GraphicsException.ShaderCompileFailed(nameof(shaderType), _gl.GetShaderInfoLog(shader));

        return shader;
    }


    public void SetUniform(int uniform, int value) => _gl.ProgramUniform1(_activeProg.Value, uniform, value);
    public void SetUniform(int uniform, uint value) => _gl.ProgramUniform1(_activeProg.Value, uniform, value);
    public void SetUniform(int uniform, float value) => _gl.ProgramUniform1(_activeProg.Value, uniform, value);

    public void SetUniform(int uniform, Vector2 value) =>
        _gl.ProgramUniform2(_activeProg.Value, uniform, value.X, value.Y);

    public void SetUniform(int uniform, in Vector3 value) => _gl.ProgramUniform3(_activeProg.Value, uniform, value);
    public void SetUniform(int uniform, in Vector4 value) => _gl.ProgramUniform4(_activeProg.Value, uniform, value);

    public unsafe void SetUniform(int uniform, in Matrix4x4 value)
    {
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix4(uniform, 1, false, p);
    }

    public unsafe void SetUniform(int uniform, in Matrix3 value)
    {
        var p = (float*)Unsafe.AsPointer(ref Unsafe.AsRef(in value));
        _gl.UniformMatrix3(uniform, 1, false, p);
    }
}