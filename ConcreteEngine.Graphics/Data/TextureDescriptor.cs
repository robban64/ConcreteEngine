#region

using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Data;

public readonly struct TextureDescriptor(byte[] pixelData, int width, int height, EnginePixelFormat format)
{
    public byte[] PixelData { get; } = pixelData;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public EnginePixelFormat Format { get; } = format;

    /*
    public EnginePixelFormat GetPixelFormat()
    {
        return Channels switch
        {
            1 => EnginePixelFormat.Red,
            3 => EnginePixelFormat.Rgb,
            4 => EnginePixelFormat.Rgba,
            _ => throw new NotSupportedException($"Unsupported channel count: {Channels}")
        };
    }
    */
}