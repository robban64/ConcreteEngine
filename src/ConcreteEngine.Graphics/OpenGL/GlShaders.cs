using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaders
{
    private static GL Gl => GlBackendDriver.Gl;

    private static readonly Dictionary<uint, string> UniformSamplerByHash = new(16);

    private readonly BackendResourceStore _shaderStore = GfxRegistry.GetBackendStore<ShaderMeta>();

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

        NativeHandle handle = default;
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

        return _shaderStore.Add(handle);
    }

    private static NativeHandle CreateShaderProgram(uint vertexShader, uint fragmentShader)
    {
        var program = Gl.CreateProgram();
        Gl.AttachShader(program, vertexShader);
        Gl.AttachShader(program, fragmentShader);
        Gl.LinkProgram(program);

        Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
        if (status != (int)GLEnum.True)
            throw GraphicsException.ShaderLinkFailed(program.ToString(), Gl.GetProgramInfoLog(program));

        return new NativeHandle(program);
    }

    private static unsafe uint CompileShader(ShaderType shaderType, NativeView<byte> source)
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


    public void GetSamplersFromProgram(GfxHandle shaderRef, List<GfxUniformSampler> result)
    {
        if (!shaderRef.IsValid) Throwers.InvalidArgument(nameof(shaderRef));

        ArgumentNullException.ThrowIfNull(result);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(result.Count, 1, nameof(result));

        Span<byte> buffer = stackalloc byte[128];
        Span<uint> length = stackalloc uint[1];
        Span<int> size = stackalloc int[1];
        Span<GLEnum> types = stackalloc GLEnum[1];

        var handle = _shaderStore.Get(shaderRef);
        Gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out var uniformsLength);
        for (uint i = 0; i < uniformsLength; i++)
        {
            Gl.GetActiveUniform(handle, i, (uint)buffer.Length, length, size, types, buffer);

            var uniformType = types[0].ToGfxUniformType();
            if (uniformType == GfxUniformType.Unknown) continue;
            var nameSpan = buffer.Slice(0, (int)length[0]);
            var hash = HashMath.HashFnv(nameSpan);

            if (!UniformSamplerByHash.TryGetValue(hash, out var strName))
            {
                strName = Encoding.UTF8.GetString(nameSpan);
                UniformSamplerByHash.Add(hash, strName);
            }

            result.Add(new GfxUniformSampler(strName, (byte)result.Count, uniformType));
        }
    }
}