#region

using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL.Utilities;

public class OpenGlException(GLEnum errorCode) : Exception($"OpenGL Error: {errorCode}");