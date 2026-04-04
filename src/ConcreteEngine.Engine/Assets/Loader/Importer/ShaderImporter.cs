using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Engine.Configuration.IO;
using static ConcreteEngine.Core.Engine.Configuration.EnginePath;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

internal sealed unsafe class ShaderImporter
{
    public const int ShaderBlockSize = 8192;
    public const int MinBlockSize = 4096;

    private static ReadOnlySpan<byte> Identifier => "@import "u8;

    private sealed class UboDictEntry(int slot, byte[] data)
    {
        public readonly int Slot = slot;
        public readonly byte[] Data = data;
    }

    private int _uboSlot;

    private readonly Dictionary<string, UboDictEntry> _uboDict = new(16);
    private readonly Dictionary<string, byte[]> _structsDict = new(4);

    public void ImportAllDefinitions()
    {
        var buffer = stackalloc byte[2048];
        var sw = new UnsafeSpanWriter(buffer, 1024);
        var line = new Span<byte>(buffer + 1024, 1024);
        ParseShaderDef("ubo.glsl", "uniform"u8, line, sw, &UboCallback);
        ParseShaderDef("structs.glsl", "struct"u8, line, sw, &StructCallback);
    }

    public ReadOnlySpan<byte> ImportShader(string path, NativeViewPtr<byte> buffer, out long length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, MinBlockSize, nameof(buffer));
        if (!File.Exists(path)) throw new FileNotFoundException("Shader Path not found.", path);

        using var fs = File.OpenRead(path);
        using var bs = new BufferedStream(fs, 8192);
        length = fs.Length;

        var sw = new UnsafeSpanWriter(buffer);
        Span<byte> line = stackalloc byte[1024];

        var cursor = 0;
        while (ReadLine(bs, line, ref cursor))
        {
            ParseShader(line.Slice(0, cursor), ref sw);
            cursor = 0;
        }

        if (cursor > 0)
            ParseShader(line.Slice(0, cursor), ref sw);

        return sw.EndSpan();
    }

    public void ClearCache()
    {
        _uboDict.Clear();
        _structsDict.Clear();
        _uboSlot = 0;
    }


    private void ParseShader(Span<byte> line, ref UnsafeSpanWriter sb)
    {
        if (sb.BytesLeft < line.Length || sb.BytesLeft < 16)
            throw new InsufficientMemoryException("Insufficient memory for loading shader, increase limit");

        line = line.TrimWhitespace();
        if (line.IsEmpty || line.StartsWith("//"u8))
        {
            sb.Append('\n');
            return;
        }

        if (line.StartsWith(Identifier))
        {
            line = line.Slice(Identifier.Length);
            var s = line.Split((byte)':');
            var type = s.MoveNext() ? line[s.Current] : throw new InvalidOperationException();
            var name = s.MoveNext() ? line[s.Current] : throw new InvalidOperationException();
            var strName = Encoding.UTF8.GetString(name);

            if (type.SequenceEqual("ubo"u8))
            {
                var uboEntry = _uboDict[strName];
                sb.Append("layout(std140, binding = "u8).Append(uboEntry.Slot).Append(") "u8);
                sb.Append(uboEntry.Data).Append('\n');
            }
            else if (type.SequenceEqual("struct"u8))
            {
                sb.Append(_structsDict[strName]).Append('\n');
            }
            else
            {
                throw new InvalidOperationException(nameof(type));
            }

            return;
        }

        var commentIdx = line.IndexOf("//"u8);
        if (commentIdx > 0)
        {
            sb.Append(line.Slice(0, commentIdx)).Append('\n');
            return;
        }

        sb.Append(line).Append('\n');
    }


    
    public void ParseShaderDef(
        string filename,
        ReadOnlySpan<byte> identifier,
        Span<byte> line,
        UnsafeSpanWriter sw,
        delegate*<string, byte[], ShaderImporter, void> onAdd
    )
    {
        using var fs = File.OpenRead(Path.Join(ShaderDefCorePath, filename));
        using var bs = new BufferedStream(fs, 8192);

        string? activeName = null;

        var cursor = 0;
        while (ReadLine(bs, line, ref cursor))
        {
            var span = line.Slice(0, cursor).TrimWhitespace();
            cursor = 0;

            if (span.IsEmpty) continue;

            if (span.StartsWith(identifier))
            {
                activeName = ExtractName(span);
                sw.Append(span);
                sw.Append('\n');
            }

            if (activeName == null) continue;

            var fieldEnd = span.IndexOf((byte)';');
            if (fieldEnd < 0) continue;

            sw.Append(span.Slice(0, fieldEnd + 1));
            sw.Append('\n');

            if (span.IndexOf("};"u8) >= 0)
            {
                if (activeName == null) throw new InvalidOperationException("Invalid shader def");
                onAdd(activeName, sw.EndSpan().ToArray(), this);

                activeName = null;
                sw.Clear();
            }
        }

        if (cursor > 0 && activeName != null && line.Slice(0, cursor).IndexOf("};"u8) >= 0)
        {
            sw.Append(line.Slice(0, cursor));
            onAdd(activeName, sw.EndSpan().ToArray(), this);
        }
    }
    
    private static void StructCallback(string name, byte[] data, ShaderImporter importer) =>
        importer._structsDict.Add(name, data);

    private static void UboCallback(string name, byte[] data, ShaderImporter importer) =>
        importer._uboDict.Add(name, new UboDictEntry(importer._uboSlot++, data));

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ReadLine(BufferedStream bs, Span<byte> line, scoped ref int cursor)
    {
        int b;
        while ((b = bs.ReadByte()) != -1)
        {
            if (b == '\n')
            {
                if (cursor > 0 && line[cursor - 1] == '\r') cursor--;
                return true;
            }

            line[cursor++] = (byte)b;
        }

        return false;
    }

    private static string ExtractName(ReadOnlySpan<byte> line)
    {
        var s = line.SplitAny((byte)' ');
        _ = s.MoveNext() ? line[s.Current] : ReadOnlySpan<byte>.Empty;
        var name = s.MoveNext() ? line[s.Current] : ReadOnlySpan<byte>.Empty;

        if (name.Length < 3)
            throw new InvalidOperationException("Shader def name require least 3 characters");

        return Encoding.UTF8.GetString(name);
    }
}
