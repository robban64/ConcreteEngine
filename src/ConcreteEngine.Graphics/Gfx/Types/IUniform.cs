namespace ConcreteEngine.Graphics.Gfx;

public interface IUniform
{
    static abstract UniformBufferId UboId { get; set; }
    static abstract byte Slot { get; }
    static abstract int Stride { get; }
}