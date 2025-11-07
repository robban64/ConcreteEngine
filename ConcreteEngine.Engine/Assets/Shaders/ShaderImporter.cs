#region

using System.Text;
using ConcreteEngine.Engine.Assets.IO;

#endregion

namespace ConcreteEngine.Engine.Assets.Shaders;

internal sealed class ShaderImporter
{
    private const string Identifier = "@import ";

    private static string UboPath => AssetPaths.CoreShaderPath("definitions", "ubo.glsl");
    private static string StructPath => AssetPaths.CoreShaderPath("definitions", "structs.glsl");

    private StringBuilder? _sb;

    private readonly Dictionary<string, (int, string)> _uboDict = new(8);
    private readonly Dictionary<string, string> _structsDict = new(4);

    private int _uboSlot = 0;

    internal ShaderImporter()
    {
    }


    public void ImportAllDefinitions()
    {
        _sb ??= new StringBuilder(8192);
        _sb.Clear();

        ImportUboDefs(UboPath);
        ImportStructDefs(StructPath);
        _sb.Clear();
    }

    public void ImportUboDefs(string path)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        ParserMethods.ParseShaderDef(sr, "uniform", _sb, UboCallback);
        _sb!.Clear();
        return;
        void UboCallback(string name, string content) => _uboDict.Add(name, (_uboSlot++, content));
    }

    public void ImportStructDefs(string path)
    {
        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        ParserMethods.ParseShaderDef(sr, "struct", _sb, StructCallback);
        return;
        void StructCallback(string name, string content) => _structsDict.Add(name, content);
    }

    public string ImportShader(string path, string? cacheName = null)
    {
        _sb ??= new StringBuilder(8192);
        _sb.Clear();

        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        var result = ParserMethods.ParseShader(sr, _sb, AppendUbo, AppendStruct);
        _sb.Clear();
        return result;
    }

    private void AppendUbo(string name)
    {
        var (slot, content) = _uboDict[name];
        _sb!.Append($"layout(std140, binding = {slot}) ");
        _sb.Append(content);
        _sb.Append('\n');
    }

    private void AppendStruct(string name)
    {
        _sb!.Append(_structsDict[name]);
        _sb.Append('\n');
    }

    public void ClearCache()
    {
        _sb = null;
        _uboDict.Clear();
        _structsDict.Clear();
        _uboSlot = 0;
    }

    private static class ParserMethods
    {
        public static string ParseShader(StreamReader sr, StringBuilder sb, Action<string> appendUbo,
            Action<string> appendStruct)
        {
            while (sr.ReadLine() is { } line)
            {
                var span = line.AsSpan();
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
                        case "ubo": appendUbo(name); break;
                        case "struct": appendStruct(name); break;
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

            return sb.ToString();
        }

        public static void ParseShaderDef(
            StreamReader sr,
            string identifier,
            StringBuilder sb,
            Action<string, string> onAdd)
        {
            string? activeName = null;
            while (sr.ReadLine() is { } line)
            {
                var span = line.AsSpan();
                if (span.IsEmpty) continue;

                if (span.StartsWith(identifier))
                {
                    activeName = ExtractName(span);
                    sb.Append(span);
                    sb.Append('\n');
                }

                if (activeName == null) continue;

                var fieldEnd = span.IndexOf(";", StringComparison.OrdinalIgnoreCase);
                if (fieldEnd < 0) continue;

                sb.Append(span.Slice(0, fieldEnd + 1));
                sb.Append('\n');

                if (span.Contains("};", StringComparison.OrdinalIgnoreCase))
                {
                    if (activeName == null) throw new InvalidOperationException("Invalid shader def");

                    onAdd(activeName, sb.ToString());
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