using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Engine.Configuration.IO;

namespace ConcreteEngine.Engine.Assets.Loader.Importer;

internal sealed class ShaderImporter : IDisposable
{
    private const string Identifier = "@import ";
    private const int ShaderBlockSize = 8192;
    private const int ShaderMinBlockSize = 2048;

    private class UboDictEntry(int slot, string name)
    {
        public readonly int Slot = slot;
        public readonly string Name = name;
    }

    private NativeArray<byte> _buffer;

    private readonly Dictionary<string, UboDictEntry> _uboDict = new(16);
    private readonly Dictionary<string, string> _structsDict = new(4);

    private int _uboSlot;

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

    public unsafe void ImportShader(string vertexPath, string fragmentPath, out ReadOnlySpan<byte> vs,
        out ReadOnlySpan<byte> fs)
    {
        if (_buffer.IsNull) _buffer = NativeArray.Allocate<byte>(ShaderBlockSize * 2);

        vs = ReadShader(vertexPath, new UnsafeSpanWriter(_buffer.Ptr, _buffer.Capacity));

        var remainingCapacity = _buffer.Capacity - vs.Length;
        if (remainingCapacity < ShaderMinBlockSize)
            throw new InsufficientMemoryException("Insufficient memory for loading shader, increase limit");

        fs = ReadShader(fragmentPath, new UnsafeSpanWriter(_buffer.Ptr + vs.Length, remainingCapacity));
    }

    private ReadOnlySpan<byte> ReadShader(string path, UnsafeSpanWriter sw)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        return ParserMethods.ParseShader(sr, sw, this);
    }

    private static void StructCallback(string name, string content, ShaderImporter importer) =>
        importer._structsDict.Add(name, content);

    private static void UboCallback(string name, string content, ShaderImporter importer) =>
        importer._uboDict.Add(name, new UboDictEntry(importer._uboSlot++, content));

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

    private static class ParserMethods
    {
        public static ReadOnlySpan<byte> ParseShader(StreamReader sr, UnsafeSpanWriter sb, ShaderImporter importer)
        {
            while (sr.ReadLine() is { } line)
            {
                var span = line.AsSpan();
                if (sb.BytesLeft < span.Length || sb.BytesLeft < 16)
                    throw new InsufficientMemoryException("Insufficient memory for loading shader, increase limit");

                if (span.IsEmpty || span.StartsWith("//"))
                {
                    sb.Append('\n');
                    continue;
                }

                if (span.StartsWith(Identifier))
                {
                    span = span.Slice(Identifier.Length);
                    var s = span.Split(':');
                    var type = s.MoveNext() ? span[s.Current] : throw new InvalidOperationException();
                    var name = s.MoveNext() ? span[s.Current].ToString() : throw new InvalidOperationException();

                    switch (type)
                    {
                        case "ubo":
                            var uboEntry = importer._uboDict[name];
                            sb.Append("layout(std140, binding = ").Append(uboEntry.Slot).Append(") ");
                            sb.Append(uboEntry.Name);
                            sb.Append('\n');
                            break;
                        case "struct":
                            sb.Append(importer._structsDict[name]);
                            sb.Append('\n');
                            break;
                        default: throw new InvalidOperationException(nameof(type));
                    }

                    continue;
                }

                var commentIdx = span.IndexOf("//", StringComparison.Ordinal);
                if (commentIdx > 0)
                {
                    sb.Append(span.Slice(0, commentIdx));
                    sb.Append('\n');
                    continue;
                }

                sb.Append(span);
                sb.Append('\n');
            }

            return sb.EndSpan();
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