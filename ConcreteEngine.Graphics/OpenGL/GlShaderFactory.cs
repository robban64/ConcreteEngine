using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlShaderFactory(GlGraphicsContext gfx, DeviceCapabilities caps)
{
    private readonly GL _gl = gfx.Gl;
    private readonly DeviceCapabilities _caps = caps;

    public delegate void SaveDelegate(GlUniformBufferHandle handle, UniformBufferMeta meta);

    public void InitializeUniformBuffers(SaveDelegate save)
    {
        var length = GraphicsEnumCache.ShaderBufferUniformVals.Length;

        Span<(UniformGpuSlot, GlUniformBufferHandle)> handles =
            stackalloc (UniformGpuSlot, GlUniformBufferHandle)[length];

        handles[0] = CreateAndSaveUniformBuffer<FrameUniformGpuData>(UniformGpuSlot.Frame, save);
        handles[1] = CreateAndSaveUniformBuffer<CameraUniformGpuData>(UniformGpuSlot.Camera, save);
        handles[2] = CreateAndSaveUniformBuffer<DirLightUniformGpuData>(UniformGpuSlot.DirLight, save);
        handles[3] = CreateAndSaveUniformBuffer<MaterialUniformGpuData>(UniformGpuSlot.Material, save);
        handles[4] = CreateAndSaveUniformBuffer<DrawObjectUniformGpuData>(UniformGpuSlot.DrawObject, save);

        foreach (var (slot, handle) in handles)
        {
            _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)slot, handle.Handle);
        }
    }
    private (UniformGpuSlot, GlUniformBufferHandle) CreateAndSaveUniformBuffer<T>(UniformGpuSlot slot,
        SaveDelegate save) where T : unmanaged, IUniformGpuData
    {
        var handle = CreateUniformBuffer<T>(slot, out var meta);
        save(handle, meta);
        return (slot, handle);
    }

    private unsafe GlUniformBufferHandle CreateUniformBuffer<T>(UniformGpuSlot slot, out UniformBufferMeta meta)
        where T : unmanaged, IUniformGpuData
    {
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout<T>();

        uint size = (uint)Unsafe.SizeOf<T>();
        meta = new UniformBufferMeta(slot: slot, blockSize: size);

        nuint capacity = UniformBufferUtils.StrideOf<T>();
        var handle = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, handle);
        _gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.StaticDraw);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, meta.BindingIdx, handle);
        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        return new GlUniformBufferHandle(handle);

    }

    public GlShaderHandle CreateShader(string vertexSource, string fragmentSource,
        out UniformTable uniformTable, out ShaderMeta meta)
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        uint handle = CreateShaderProgram(vertexShader, fragmentShader);
        _gl.DetachShader(handle, vertexShader);
        _gl.DetachShader(handle, fragmentShader);
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);


        _gl.UseProgram(handle);
        GetUniformsFromProgram(handle, out var uniformPair, out var samplers);
        uniformTable = new UniformTable(uniformPair);
        _gl.UseProgram(0);

        meta = new ShaderMeta((uint)samplers);
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

    private void GetUniformsFromProgram(uint handle, out List<(string, int)> uniforms, out int samplers)
    {
        _gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        uniforms = new List<(string, int)>(uniformsLength);
        samplers = 0;
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = _gl.GetActiveUniform(handle, uniformIndex, out _, out var type);
            int uniformLocation = _gl.GetUniformLocation(handle, uniformName);
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