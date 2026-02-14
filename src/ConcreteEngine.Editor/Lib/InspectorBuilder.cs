using System.Collections;
using System.Diagnostics;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Lib;

public static class InspectorBuilder
{
    internal static UnsafeSpanWriter Writer;

    private static readonly Stopwatch Sw = new();

    public static void Build(Type type, object target, List<Row> rows)
    {
        Sw.Start();
        InspectorRegistry.TryGet(type, out var entries);
        foreach (var entry in entries)
        {
            var value = entry.Getter(target);
            BuildRecursive(entry, value, rows, 0);
        }

        Sw.Stop();
        Console.WriteLine($"Time {Sw.ElapsedTicks / 1000.0}");
        Sw.Reset();
    }

    private static void BuildRecursive(InspectorFieldMeta meta, object? target, List<Row> rows, int depth = 0)
    {
        if (target == null)
        {
            rows.Add(Row.Make("(null)"u8, "null"u8, depth));
            return;
        }

        // Leaf types
        if (meta.TypeKind is InspectorTypeKind.Primitive or InspectorTypeKind.String)
        {
            rows.Add(Row.Make(meta.Name, FormatValue(target, default), depth));
            return;
        }

        // Dictionary
        if (meta.TypeKind == InspectorTypeKind.Map)
        {
            var dict = (IDictionary)target;
            rows.Add(Row.Make(meta.Name, ReadOnlySpan<byte>.Empty, depth));
            foreach (DictionaryEntry kv in dict)
            {
                var value = kv.Value;
                if (value == null || !InspectorRegistry.TryGet(value.GetType(), out var entries))
                {
                    var keyLabel = kv.Key as string ?? kv.Key.ToString() ?? "null";
                    rows.Add(Row.Make(keyLabel, FormatValue(value, default), depth + 1));
                    continue;
                }

                foreach (var it in entries)
                {
                    BuildRecursive(it, it.Getter(value), rows, depth + 1);
                }
            }

            return;
        }

        // Collection (array, list)
        if (meta.TypeKind == InspectorTypeKind.Array)
        {
            var list = (IList)target;
            var index = 0;

            rows.Add(Row.Make(meta.Name, ReadOnlySpan<byte>.Empty, depth));
            foreach (var item in list)
            {
                if (!InspectorRegistry.TryGet(item.GetType(), out var entries))
                {
                    rows.Add(Row.Make($"[{index}]", FormatValue(item, default), depth + 1));
                    continue;
                }

                foreach (var it in entries)
                {
                    var value = it.Getter(item);
                    BuildRecursive(it, value, rows, depth + 1);
                }

                index++;
            }

            return;
        }

        rows.Add(Row.Make(meta.Type.Name, ReadOnlySpan<byte>.Empty, depth));

        if (!InspectorRegistry.TryGet(meta.Type, out var childEntries)) return;

        foreach (var it in childEntries)
        {
            var value = it.Getter(target);
            if (it.TypeKind is InspectorTypeKind.Class or InspectorTypeKind.Struct
                or InspectorTypeKind.PrimitiveStruct)
            {
                BuildRecursive(it, value, rows, depth + 1);
            }
            else
            {
                var valueUtf8 = FormatValue(value, new FormatOptions(it.Format, it.TypeKind));
                rows.Add(Row.Make(it.Name, valueUtf8, depth + 1));
            }
        }
    }


    private static ReadOnlySpan<byte> FormatValue(object? target, in FormatOptions formatOptions)
    {
        if (target == null) return "null"u8;

        var format = formatOptions.Format;

        if (target is bool bol) return FormatBool(bol, format);

        var sw = Writer;
        switch (target)
        {
            case byte b: sw.Start((int)b); break;
            case short s: sw.Start((int)s); break;
            case ushort us: sw.Start((int)us); break;
            case int i: sw.Start(i); break;
            case uint ui: sw.Start(ui); break;
            case float f: sw.Start(f, format); break;
            case double d: sw.Start(d, format); break;
            case long l: sw.Start(l, format); break;
            case ulong ul: sw.Start(ul, format); break;
            case Guid g: sw.Start(g, format); break;
            case DateTime dt: sw.Start(dt, format); break;
            case string s: sw.Start(s); break;
            default: sw.Start(target.ToString() ?? "null"); break;
        }

        return sw.EndSpan();

        static ReadOnlySpan<byte> FormatBool(bool value, string? format)
        {
            if (!string.IsNullOrWhiteSpace(format) && format.Length >= 3 && !char.IsWhiteSpace(format[0]))
            {
                var span = format.AsSpan();
                var splitIndex = span.IndexOf('/');
                if (splitIndex >= 0)
                {
                    return Writer.Start(value ? span.Slice(0, splitIndex) : span.Slice(splitIndex + 1)).EndSpan();
                }
            }

            return Writer.Start(value ? "true" : "false").EndSpan();
        }
    }
}