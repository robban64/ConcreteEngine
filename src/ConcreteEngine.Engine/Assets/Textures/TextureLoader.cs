using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Renderer.Definitions;
using StbImageSharp;

namespace ConcreteEngine.Engine.Assets.Textures;

internal sealed class TextureLoader(AssetGfxUploader uploader)
{
    public TextureImportResult LoadEmbeddedTexture(TextureEmbeddedDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.PixelData);
        ArgumentOutOfRangeException.ThrowIfLessThan(descriptor.PixelData.Length, 4);

        var image = ImageResult.FromMemory(descriptor.PixelData, GetColorComponent(descriptor.PixelFormat));
        ValidateImageResult(image);

        if (descriptor.Width != descriptor.PixelData.Length && descriptor.Height != 0)
        {
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, descriptor.Width, nameof(image.Width));
            ArgumentOutOfRangeException.ThrowIfNotEqual(image.Width, descriptor.Height, nameof(image.Width));
        }

        var settings = EngineSettings.Instance.Graphics;

        var desc = new CreateTextureInfo(
            width: image.Width,
            height: image.Height,
            kind: TextureKind.Texture2D,
            format: descriptor.PixelFormat
        );

        var props = new CreateTextureProps(
            preset: TexturePreset.LinearMipmapRepeat,
            anisotropy: descriptor.SlotKind == TextureSlotKind.Albedo ? settings.MaxAnisotropy : TextureAnisotropy.Off,
            lodBias: 0
        );

        var meta = new TextureUploadMeta(desc, props);

        var data = image.Data;
        uploader.UploadTexture(data, meta, out var info);

        return new TextureImportResult
        {
            Data = null,
            CreationInfo = info,
            TextureDesc = desc,
            TextureProps = props,
            FileSize =  data.Length
        };
    }

    public TextureImportResult LoadTexture(TextureDescriptor record)
    {
        //StbImage.stbi_set_flip_vertically_on_load(1);

        var path = Path.Combine(EnginePath.TexturePath, record.Filename);

        var fi = new FileInfo(path);
        if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

        using var stream = File.OpenRead(path);


        var image = ImageResult.FromStream(stream, GetColorComponent(record.PixelFormat));
        ValidateImageResult(image);

        var desc = new CreateTextureInfo(
            width: image.Width,
            height: image.Height,
            kind: TextureKind.Texture2D,
            format: record.PixelFormat
        );

        var props = new CreateTextureProps(
            preset: record.Preset,
            anisotropy: GetAnisotropy(record.Anisotropy),
            lodBias: record.LodBias
        );

        //Console.WriteLine(props.Anisotropy.ToString());

        var meta = new TextureUploadMeta(desc, props);
        uploader.UploadTexture(image.Data, meta, out var info);

        return new TextureImportResult
        {
            FileSize = fi.Length,
            Data = record.InMemory ? image.Data : null,
            CreationInfo = info,
            TextureDesc = desc,
            TextureProps = props
        };
    }

    public CubeMapImportResult LoadCubeMap(CubeMapDescriptor record, Span<FileSpecArgs> args)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(record.Textures.Length, 6);

        var faceData = new ReadOnlyMemory<byte>[6];
        var faceFiles = new AssetFileSpec[6];

        for (int i = 0; i < 6; i++)
        {
            var path = Path.Combine(EnginePath.CubeMapPath, record.Textures[i]);

            var fi = new FileInfo(path);
            if (!fi.Exists) throw new FileNotFoundException("File not found.", path);

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            ValidateImageResult(image, record);

            faceData[i] = image.Data;
            var arg = args[i];
            faceFiles[i] = new AssetFileSpec(
                Id: arg.Id,
                GId:arg.GId,
                Storage: AssetStorageKind.FileSystem,
                LogicalName: record.Name,
                RelativePath: record.Textures[i],
                SizeBytes: fi.Length);
        }

        var desc = new CreateTextureInfo(
            width: record.Width,
            height: record.Height,
            kind: TextureKind.CubeMap,
            format: record.PixelFormat);

        var props = new CreateTextureProps(
            preset: record.Preset,
            anisotropy: TextureAnisotropy.Off,
            lodBias: 0);


        var payload = new TextureUploadMeta(desc, props);
        uploader.UploadCubeMap(faceData, payload, out var info);


        return new CubeMapImportResult
        {
            FaceFiles = faceFiles, CreationInfo = info, TextureDesc = desc, TextureProps = props
        };
    }

    private static TextureAnisotropy GetAnisotropy(TextureAnisotropyProfile format)
    {
        return format switch
        {
            TextureAnisotropyProfile.Default => EngineSettings.Instance.Graphics.MaxAnisotropy,
            TextureAnisotropyProfile.Off => TextureAnisotropy.Off,
            TextureAnisotropyProfile.X2 => TextureAnisotropy.X2,
            TextureAnisotropyProfile.X4 => TextureAnisotropy.X4,
            TextureAnisotropyProfile.X8 => TextureAnisotropy.X8,
            TextureAnisotropyProfile.X16 => TextureAnisotropy.X16,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static ColorComponents GetColorComponent(TexturePixelFormat format)
    {
        return format switch
        {
            TexturePixelFormat.Rgb => ColorComponents.RedGreenBlueAlpha,
            TexturePixelFormat.Rgba => ColorComponents.RedGreenBlueAlpha,
            TexturePixelFormat.SrgbAlpha => ColorComponents.RedGreenBlueAlpha,
            TexturePixelFormat.Depth => ColorComponents.Grey,
            TexturePixelFormat.Red => ColorComponents.Grey,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static void ValidateImageResult(ImageResult result)
    {
        ArgumentNullException.ThrowIfNull(result, nameof(result));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Width, 0, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(result.Height, 0, nameof(result.Height));
    }

    private static void ValidateImageResult(ImageResult result, CubeMapDescriptor record)
    {
        ValidateImageResult(result);
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Width, record.Width, nameof(result.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(result.Height, record.Height, nameof(result.Height));
    }
}