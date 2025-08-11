#region

using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.Error;

public class OpenGlException(GLEnum errorCode) : Exception($"OpenGL Error: {errorCode}");