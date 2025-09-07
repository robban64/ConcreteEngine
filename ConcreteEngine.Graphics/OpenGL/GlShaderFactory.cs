using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaderFactory(GlGraphicsContext gfx, DeviceCapabilities caps)
{
    private readonly GL _gl = gfx.Gl;
    private readonly DeviceCapabilities _caps = caps;
    
    public GlShaderHandle CreateShader(
        string vertexSource,
        string fragmentSource,
        string[]? samplers,
        out UniformTable uniformTable,
        out ShaderMeta meta
    )
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        uint handle = CreateShaderProgram(vertexShader, fragmentShader);
        _gl.DetachShader(handle, vertexShader);
        _gl.DetachShader(handle, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);


        _gl.UseProgram(handle);
        var uniformDict = GetUniformsFromProgram(handle);
        uniformTable = new UniformTable(uniformDict);

        if (samplers?.Length > 0)
        {
            for (int i = 0; i < samplers.Length; i++)
                _gl.Uniform1(uniformTable.GetUniformLocation(samplers[i]), i);
        }
        _gl.UseProgram(0);


        var samplerLength = samplers != null ? (uint)samplers.Length : 0;
        meta = new ShaderMeta(samplerLength);
        return new GlShaderHandle(handle);
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
    
    private List<(string, int)> GetUniformsFromProgram(uint handle)
    {
        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        var uniforms = new List<(string, int)>(uniformsLength);

        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = _gl.GetActiveUniform(handle, uniformIndex, out _, out _);
            int uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add((uniformName, uniformLocation));
            }
        }

        return uniforms;
    }
/*
    private Dictionary<string, int> GetUniformsFromProgram(uint handle)
    {
        var uniforms = new Dictionary<string, int>();

        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = _gl.GetActiveUniform(handle, uniformIndex, out _, out _);
            int uniformLocation = _gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add(uniformName, uniformLocation);
            }
        }

        return uniforms;
    }
*/
}