namespace ConcreteEngine.Graphics.Gfx;

public interface IUniform
{
    static abstract int DrawCursor { get; set; }
    static abstract int UploadCursor { get; set; }

    static abstract UniformBufferId UboId { get; set; }
    static abstract byte Slot { get; }
    static abstract int OverrideSize { get; }
}