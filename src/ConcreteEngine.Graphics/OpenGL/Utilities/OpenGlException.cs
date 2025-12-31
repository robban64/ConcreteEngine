using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL.Utilities;

public sealed class OpenGlException(GLEnum errorCode) : Exception($"OpenGL Error: {errorCode}");