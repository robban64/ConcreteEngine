using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics;

public static class GraphicsEnumCache
{
    public static readonly ShaderUniform[] ShaderUniformVals = Enum.GetValues<ShaderUniform>();
    public static readonly UniformGpuSlot[] ShaderBufferUniformVals = Enum.GetValues<UniformGpuSlot>();

}