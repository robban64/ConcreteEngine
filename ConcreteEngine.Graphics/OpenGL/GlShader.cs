#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Data;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

public sealed class GlShader : OpenGLResource, IShader
{
    private readonly UniformTable _uniforms;

    internal GlShader(uint handle, Dictionary<string, int> uniforms) : base(handle)
    {
        _uniforms = new UniformTable(uniforms);
    }

    public int this[ShaderUniform u]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _uniforms[u];
    }

/*
    internal GlShader(GlGraphicsContext ctx, string vertexSource, string fragmentSource) : base(ctx)
    {
        uint vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
        uint fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);

        Handle = CreateShaderProgram(vertexShader, fragmentShader);
        this.ValidateResourceCreated();

        Uniforms = GetUniformsFromProgram(Handle).AsReadOnly();

        LocModelMatrix = Uniforms.GetValueOrDefault(ModelUniformName, -1);
        LocProjectionViewMatrix = Uniforms.GetValueOrDefault(ProjectionViewUniformName, -1);
        LocSampleTexture = Uniforms.GetValueOrDefault(SamplerTextureUniformName, -1);
        LocTextureOffset = Uniforms.GetValueOrDefault(TextureOffsetUniformName, -1);
        LocTextureScale = Uniforms.GetValueOrDefault(TextureScaleUniformName, -1);


        Gl.DetachShader(Handle, vertexShader);
        Gl.DetachShader(Handle, fragmentShader);
        Gl.DeleteShader(vertexShader);
        Gl.DeleteShader(fragmentShader);

        IsReady = true;
    }
*/

/*
    public void SetTextureIndex(int textureIndex = 0)
    {
        SetUniform(LocSampleTexture, textureIndex);
    }

    public void SetProjectionView(in Matrix4X4<float> projectionView)
    {
        SetUniform(LocProjectionViewMatrix, projectionView);
    }

    public void SetTransform(in Matrix4X4<float> transform)
    {
        SetUniform(LocModelMatrix, transform);
    }

    public void SetTextureOffset(Vector2D<float> value)
    {
        SetUniform(LocTextureOffset, value);
    }

    public void SetTextureScale(Vector2D<float> value)
    {
        SetUniform(LocTextureScale, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int location, int value) => _gl.Uniform1(location, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int location, uint value) => _gl.Uniform1(location, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int location, float value) => _gl.Uniform1(location, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int location, Vector2D<float> value) => _gl.Uniform2(location, value.X, value.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int location, Vector3D<float> value)
    {
        _gl.Uniform3(location, value.X, value.Y, value.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniform(int location, Vector4D<float> value)
    {
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetUniform(int location, Matrix4X4<float> value)
    {
        _gl.UniformMatrix4(location, 1, false, (float*)&value);
    }


    private static Dictionary<string, int> GetUniformsFromProgram(GL gl, uint handle)
    {
        var uniforms = new Dictionary<string, int>();

        gl.GetProgram(handle, ProgramPropertyARB.ActiveUniforms, out int uniformsLength);
        for (uint uniformIndex = 0; uniformIndex < uniformsLength; uniformIndex++)
        {
            string uniformName = gl.GetActiveUniform(handle, uniformIndex, out _, out _);
            int uniformLocation = gl.GetUniformLocation(handle, uniformName);
            if (uniformLocation >= 0)
            {
                uniforms.Add(uniformName, uniformLocation);
            }
        }

        return uniforms;
    }
   */
}