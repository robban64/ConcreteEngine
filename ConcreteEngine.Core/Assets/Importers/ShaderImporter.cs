using System.Text;

namespace ConcreteEngine.Core.Assets.Importers;

internal sealed class ShaderImporter
{
    private const string Identifier = "@import ";

    private readonly ShaderDefProvider _defProvider;

    private StringBuilder? _sb = null;

    internal ShaderImporter()
    {
        _defProvider = new ShaderDefProvider();
    }

    private static string GetShadersPath() => Path.Combine(AssetPaths.GetAssetCorePath(), "shaders");

    public void CleanCache()
    {
        _sb = null;
    }

    public void LoadDefinitions()
    {
        _sb ??= new StringBuilder(8192);
        _sb.Clear();

        var basePath = GetShadersPath();
        _defProvider.LoadUboDefs(Path.Combine(basePath, "ubo.glsl"), _sb);
        _sb.Clear();
        _defProvider.LoadStructDefs(Path.Combine(basePath, "structs.glsl"), _sb);
        _sb.Clear();
    }

    public string ParseShader(string path, string? cacheName = null)
    {
        _sb ??= new StringBuilder(8192);
        _sb.Clear();

        using var fs = File.OpenRead(path);
        using var sr = new StreamReader(fs, Encoding.UTF8);
        while (sr.ReadLine() is { } line)
        {
            var span = line.AsSpan();
            ProcessLine(span);
        }

        var result = _sb.ToString();
        _sb.Clear();
        return result;
    }

    private void ProcessLine(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty || span.StartsWith("//"))
        {
            _sb!.Append('\n');
            return;
        }

        if (span.StartsWith(Identifier))
        {
            span = span.Slice(Identifier.Length);
            var s = span.Split(':');
            var type = s.MoveNext() ? span[s.Current] : throw new InvalidOperationException();
            var name = s.MoveNext() ? span[s.Current].ToString() : throw new InvalidOperationException();

            switch (type)
            {
                case "ubo": _defProvider.AppendUbo(name, _sb!); break;
                case "struct": _defProvider.AppendStruct(name, _sb!); break;
                default: throw new InvalidOperationException(nameof(type));
            }

            return;
        }

        var commentIdx = span.IndexOf("//", StringComparison.Ordinal);
        if (commentIdx > 0)
        {
            _sb!.Append(span.Slice(0, commentIdx));
            _sb.Append('\n');
            return;
        }

        _sb!.Append(span);
        _sb.Append('\n');
    }


    private sealed class ShaderDefProvider
    {
        private readonly Dictionary<string, (int, string)> _uboDict = new(8);
        private readonly Dictionary<string, string> _structsDict = new(4);
        private int _uboSlot = 0;

        public void AppendUbo(string name, StringBuilder sb)
        {
            var (slot, content) = _uboDict[name];
            sb.Append($"layout(std140, binding = {slot}) ");
            sb.Append(content);
            sb.Append('\n');
        }

        public void AppendStruct(string name, StringBuilder sb)
        {
            sb.Append(_structsDict[name]);
            sb.Append('\n');
        }

        public void LoadUboDefs(string path, StringBuilder sb)
        {
            using var fs = File.OpenRead(path);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            ParseDef(sr, "uniform", sb, UboCallback);
            return;
            void UboCallback(string name, string content) => _uboDict.Add(name, (_uboSlot++, content));
        }

        public void LoadStructDefs(string path, StringBuilder sb)
        {
            using var fs = File.OpenRead(path);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            ParseDef(sr, "struct", sb, StructCallback);
            return;
            void StructCallback(string name, string content) => _structsDict.Add(name, content);
        }

        private static void ParseDef(
            StreamReader sr, 
            string identifier, 
            StringBuilder sb,
            Action<string,string> onAdd)
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
            int idx = 0;

            var s = line.SplitAny(ReadOnlySpan<char>.Empty);
            var type = s.MoveNext() ? line[s.Current] : ReadOnlySpan<char>.Empty;
            var name = s.MoveNext() ? line[s.Current] : ReadOnlySpan<char>.Empty;

            if (name.Length < 3)
                throw new InvalidOperationException("Shader def name require least 3 characters");

            return name.ToString();
        }
    }
}