using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Engine.Configuration.IO;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

internal sealed class ShaderImporter : IDisposable
{
    private const int ShaderBlockSize = 8192;
    private const int ShaderMinBlockSize = 2048;
    private static ReadOnlySpan<byte> Identifier => "@import "u8;

    private sealed class UboDictEntry(int slot, string content)
    {
        public readonly int Slot = slot;
        public readonly string Content = content;
    }

    private int _uboSlot;

    private NativeArray<byte> _buffer;

    private readonly Dictionary<string, UboDictEntry> _uboDict = new(16);
    private readonly Dictionary<string, string> _structsDict = new(4);


    public void ImportAllDefinitions()
    {
        if (_buffer.IsNull) _buffer = NativeArray.Allocate<byte>(ShaderBlockSize * 2);

        ImportUboDefs(Path.Combine(EnginePath.ShaderDefCorePath, "ubo.glsl"));
        ImportStructDefs(Path.Combine(EnginePath.ShaderDefCorePath, "structs.glsl"));
    }

    private void ImportUboDefs(string path)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        ParserMethods.ParseShaderDef(sr, this, "uniform", UboCallback);
    }

    private void ImportStructDefs(string path)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        ParserMethods.ParseShaderDef(sr, this, "struct", StructCallback);
    }

    public unsafe void ImportShader(
        string vertexPath,
        string fragmentPath,
        out NativeViewPtr<byte> vs,
        out NativeViewPtr<byte> fs,
        out long vsLength,
        out long fsLength)
    {
        if (!File.Exists(vertexPath)) throw new FileNotFoundException("Vertex Path not found.", vertexPath);
        if (!File.Exists(fragmentPath)) throw new FileNotFoundException("Vertex Path not found.", fragmentPath);

        if (_buffer.IsNull) _buffer = NativeArray.Allocate<byte>(ShaderBlockSize * 2);

        var vsSpan = ReadShader(vertexPath, new UnsafeSpanWriter(_buffer.Ptr, _buffer.Length), out vsLength);

        var remainingCapacity = _buffer.Length - vsSpan.Length;
        if (remainingCapacity < ShaderMinBlockSize)
            throw new InsufficientMemoryException("Insufficient memory for loading shader, increase limit");

        var fsSpan = ReadShader(fragmentPath, new UnsafeSpanWriter(_buffer.Ptr + vsSpan.Length, remainingCapacity), out fsLength);

        vs = _buffer.Slice(0, vsSpan.Length);
        fs = _buffer.Slice(vsSpan.Length, fsSpan.Length);
    }

    public void ClearCache()
    {
        _uboDict.Clear();
        _structsDict.Clear();
        _uboSlot = 0;
    }

    public void Dispose()
    {
        _buffer.Dispose();
        _buffer = default;
    }

    ReadOnlySpan<byte> ReadShader(string path, UnsafeSpanWriter sw, out long length)
    {
        using var fs = File.OpenRead(path);
        using var bs = new BufferedStream(fs, 65536);
        length = fs.Length;
        int b = 0, cursor = 0;
        Span<byte> line = stackalloc byte[1024];
        while ((b = bs.ReadByte()) != -1)
        {
            if (b == '\n')
            {
                if (cursor > 0 && line[cursor - 1] == '\r')
                    cursor--;

                ParserMethods.ParseShader(line.Slice(0, cursor), ref sw, this);
                cursor = 0;
            }
            else
            {
                line[cursor++] = (byte)b;
            }
        }
        if (cursor > 0)
            ParserMethods.ParseShader(line.Slice(0, cursor), ref sw, this);

        return sw.EndSpan();
    }

    private static void StructCallback(string name, string content, ShaderImporter importer) =>
        importer._structsDict.Add(name, content);

    private static void UboCallback(string name, string content, ShaderImporter importer) =>
        importer._uboDict.Add(name, new UboDictEntry(importer._uboSlot++, content));

    private static class ParserMethods
    {
        public static void ParseShader(Span<byte> line, ref UnsafeSpanWriter sb, ShaderImporter importer)
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
                    var uboEntry = importer._uboDict[strName];
                    sb.Append("layout(std140, binding = "u8).Append(uboEntry.Slot).Append(") "u8);
                    sb.Append(uboEntry.Content);
                    sb.Append('\n');

                }
                else if (type.SequenceEqual("struct"u8))
                {
                    sb.Append(importer._structsDict[strName]);
                    sb.Append('\n');
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
                sb.Append(line.Slice(0, commentIdx));
                sb.Append('\n');
                return;
            }

            sb.Append(line);
            sb.Append('\n');
        }

        public static void ParseShaderDef(
            StreamReader sr,
            ShaderImporter importer,
            string identifier,
            Action<string, string, ShaderImporter> onAdd)
        {
            string? activeName = null;

            Span<char> dest = stackalloc char[1024];
            var sb = new SpanWriter(dest);
            while (sr.ReadLine() is { } line)
            {
                var span = line.AsSpan().Trim();
                if (span.IsEmpty) continue;

                if (span.StartsWith(identifier))
                {
                    activeName = ExtractName(span);
                    sb.Append(span);
                    sb.Append('\n');
                }

                if (activeName == null) continue;

                var fieldEnd = span.IndexOf(";", StringComparison.Ordinal);
                if (fieldEnd < 0) continue;

                sb.Append(span.Slice(0, fieldEnd + 1));
                sb.Append('\n');

                if (span.Contains("};", StringComparison.Ordinal))
                {
                    if (activeName == null) throw new InvalidOperationException("Invalid shader def");

                    onAdd(activeName, sb.End().ToString(), importer);
                    activeName = null;
                    sb.Clear();
                }
            }
        }

        private static string ExtractName(ReadOnlySpan<char> line)
        {
            var s = line.SplitAny(ReadOnlySpan<char>.Empty);
            var type = s.MoveNext() ? line[s.Current] : ReadOnlySpan<char>.Empty;
            var name = s.MoveNext() ? line[s.Current] : ReadOnlySpan<char>.Empty;

            if (name.Length < 3)
                throw new InvalidOperationException("Shader def name require least 3 characters");

            return name.ToString();
        }


    }
}