namespace ConcreteEngine.Graphics.Resources;

public interface IShader : IGraphicsResource
{
    //public UniformTable Uniforms { get; }
}

/*
public void SetTextureIndex(int textureIndex = 0);
public void SetProjectionView(in Matrix4X4<float> projectionView);
public void SetTransform(in Matrix4X4<float> transform);
public void SetTextureOffset(Vector2D<float> value);
public void SetTextureScale(Vector2D<float> value);

public void SetUniform(int location, int value);
public void SetUniform(int location, float value);
public void SetUniform(int location, Vector2D<float> value);
public void SetUniform(int location, Vector3D<float> value);
public void SetUniform(int location, Vector4D<float> value);
public unsafe void SetUniform(int location, Matrix4X4<float> value);
*/