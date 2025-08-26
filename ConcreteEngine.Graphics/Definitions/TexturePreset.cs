namespace ConcreteEngine.Graphics.Definitions;

public enum TexturePreset : byte
{
    NearestClamp, // Pixel-art, clamp edges (UI icons, non-repeating)
    NearestRepeat, // Pixel-art, repeating textures
    LinearClamp, // Smooth sampling, clamp edges (UI, images)
    LinearRepeat, // Smooth sampling, repeating (backgrounds, scrolling patterns)
    LinearMipmapClamp, // Smooth sampling with mipmaps, clamp edges
    LinearMipmapRepeat, // Smooth sampling with mipmaps, repeat
    PremultipliedUI // Smooth, clamp edges, for pre-multiplied alpha UI glyphs
}