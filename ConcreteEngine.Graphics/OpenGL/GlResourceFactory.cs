#region

using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal class GlResourceFactory(GlGraphicsContext ctx)
{
    private GL Gl = ctx.Gl;

    public GlMesh CreateMesh<T>(GlGraphicsDevice graphics, MeshDescriptor<T> descriptor) where T : unmanaged
    {
        var vertexBufferDesc = descriptor.VertexBuffer;
        var indexBufferDesc = descriptor.IndexBuffer;

        uint handle = Gl.GenVertexArray();
        Gl.BindVertexArray(handle);

        var vertexBuffer = graphics.CreateVertexBuffer(vertexBufferDesc.Usage);
        ctx.BindVertexBuffer(vertexBuffer);
        if (vertexBufferDesc.Data is not null)
            ctx.SetVertexBuffer<T>(vertexBufferDesc.Data.AsSpan());

        var indexBuffer = graphics.CreateIndexBuffer(indexBufferDesc.Usage);
        ctx.BindIndexBuffer(indexBuffer);
        if (indexBufferDesc.Data is not null)
            ctx.SetIndexBuffer(indexBufferDesc.Data.AsSpan());

        for (int i = 0; i < descriptor.VertexPointers.Length; i++)
        {
            var pointer = descriptor.VertexPointers[i];
            AddAttribPointer((uint)i, (int)pointer.Format, pointer.StrideBytes, pointer.OffsetBytes,
                pointer.Normalized);
        }

        Gl.BindVertexArray(0);
        ctx.BindVertexBuffer(0);
        ctx.BindIndexBuffer(0);

        var vertexBufferSize = vertexBufferDesc.Data?.Length ?? 0;
        var indexBufferSize = indexBufferDesc.Data?.Length ?? 0;
        
        uint drawCount = (uint)(indexBuffer > 0 ? indexBufferSize : vertexBufferSize);
        var mesh = new GlMesh(handle, vertexBuffer, indexBuffer, descriptor.VertexPointers, drawCount);
        return mesh;
    }

    public GlBuffer CreateBuffer(BufferTarget target, BufferUsage usage)
    {
        var handle = (ushort)Gl.GenBuffer();

        return target switch
        {
            BufferTarget.VertexBuffer => new GlVertexBuffer(handle, usage),
            BufferTarget.IndexBuffer => new GlIndexBuffer(handle, usage),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }

    public GlTexture2D CreateTexture2D(in TextureDescriptor descriptor)
    {
        uint handle = Gl.GenTexture();
        Gl.BindTexture(GLEnum.Texture2D, handle);
        var (glFormat, glInternalFormat) = descriptor.Format.ToGlEnums();
        unsafe
        {
            fixed (byte* ptr = descriptor.PixelData)
            {
                Gl.TexImage2D(GLEnum.Texture2D, 0, (int)glInternalFormat,
                    (uint)descriptor.Width, (uint)descriptor.Height, 0,
                    glFormat, GLEnum.UnsignedByte, ptr);
            }
        }

        Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
        Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Nearest);
        Gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Nearest);

        Gl.BindTexture(GLEnum.Texture2D, 0);

        var texture = new GlTexture2D(handle, descriptor.Width, descriptor.Height, descriptor.Format);
        return texture;
    }

    public (GlShader shader, uint handle, UniformTable uniformTable) CreateShader(string vertexSource, string fragmentSource)
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);
        uint handle = CreateShaderProgram(vertexShader, fragmentShader);

        var uniformDict = GetUniformsFromProgram(handle);
        var uniformTable = new UniformTable(uniformDict);
        var shader = new GlShader(handle);

        Gl.DetachShader(handle, vertexShader);
        Gl.DetachShader(handle, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        return (shader, handle, uniformTable);
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

    private Dictionary<string, int> GetUniformsFromProgram(uint handle)
    {
        var uniforms = new Dictionary<string, int>();

        Gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = Gl.GetActiveUniform(handle, uniformIndex, out _, out _);
            int uniformLocation = Gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add(uniformName, uniformLocation);
            }
        }

        return uniforms;
    }

    private unsafe void AddAttribPointer(uint index, int size, uint strideBytes, uint offsetBytes,
        bool normalized = false)
    {
        Gl.EnableVertexAttribArray(index);
        Gl.VertexAttribPointer(
            index,
            size,
            VertexAttribPointerType.Float,
            normalized,
            strideBytes,
            (void*)(offsetBytes)
        );
    }
}