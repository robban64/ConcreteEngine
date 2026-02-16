using System.Collections;
using System.Diagnostics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Editor.Lib;

public static class InspectorBuilder
{
    private static readonly Stopwatch Watch = new();

    public static unsafe InspectorEditorObject Build(Type type, object target)
    {
        Watch.Start();
        InspectorRegistry.TryGet(type, out var entries);

        byte* buffer = stackalloc byte[128];
        var sw = new UnsafeSpanWriter(buffer, 128);

        var inspector = new InspectorEditorObject(type.Name, type);
        foreach (var meta in entries)
        {
            var value = meta.Getter(target);
            if (value == null) continue;

            if (meta.TypeKind == InspectorTypeKind.Struct || meta.TypeKind == InspectorTypeKind.Class)
            {
                var properties = BuildProperties(meta, value, in sw);
                if (properties != null)
                {
                    inspector.Sections.Add(properties);
                    continue;
                }
            }

            if (meta.TypeKind == InspectorTypeKind.Array)
            {
                inspector.ArrayUi = BuildArray(meta, value, sw);
            }

            BuildHeader(inspector, meta, value, in sw);
        }

        Watch.Stop();
        Console.WriteLine($"Time {Watch.ElapsedTicks / 1000.0}");
        Watch.Reset();
        return inspector;
    }

    private static void BuildHeader(InspectorEditorObject inspector, InspectorFieldMeta meta, object? target,
        in UnsafeSpanWriter sw)
    {
        if (target == null || meta.TypeKind == InspectorTypeKind.Unknown) return;

        if (meta.FieldKind == InspectorFieldKind.Name)
            inspector.Header.Name = new String16Utf8((string)target);

        if (meta.FieldKind == InspectorFieldKind.Generation)
        {
            var value = FormatValue(target, default, sw);
            inspector.Header.Gen = new String8Utf8(value);
            return;
        }

        if (meta.FieldKind == InspectorFieldKind.Id)
        {
            inspector.Header.Id = GetPrimitiveStruct(meta, target, in sw);
        }
    }

    private static InspectorSectionUi? BuildProperties(InspectorFieldMeta meta, object target,
        in UnsafeSpanWriter sw)
    {
        if (!InspectorRegistry.TryGet(meta.Type, out var entries)) return null;

        var properties = new InspectorSectionUi(meta.Name) { Title = new String32Utf8(meta.Name) };
        foreach (var it in entries)
        {
            if (it.TypeKind != InspectorTypeKind.Primitive && it.TypeKind != InspectorTypeKind.String)
                continue;

            var value = FormatValue(it.Getter(target), new FormatOptions(it.Format, it.TypeKind), sw);
            properties.Properties.Add(new UiTextProperty(new String16Utf8(it.Name), new String16Utf8(value)));
        }

        return properties;
    }

    private static InspectorArrayUi BuildArray(InspectorFieldMeta meta, object target, UnsafeSpanWriter sw)
    {
        var list = (IList)target;
        var index = 0;
        var array = new InspectorArrayUi(meta.Name, meta.Name, list.Count);

        foreach (var item in list)
        {
            if (!InspectorRegistry.TryGet(item.GetType(), out var entries)) continue;

            var section = new InspectorSectionUi(item.GetType().Name);
            array.Fields.Add(section);

            foreach (var entry in entries)
            {
                var itemTarget = entry.Getter(item);
                if (itemTarget == null) continue;


                if (entry.FieldKind == InspectorFieldKind.Name)
                {
                    var strValue = (string)itemTarget;
                    section.Title =
                        new String32Utf8(sw.Start('[').Append(index).Append("] - ").Append(strValue).EndSpan());
                    continue;
                }

                if (entry.TypeKind == InspectorTypeKind.PrimitiveStruct)
                {
                    var idValue = new String16Utf8(GetPrimitiveStruct(entry, itemTarget, in sw).AsSpan());
                    section.Properties.Add(new UiTextProperty(new String16Utf8(entry.Name), idValue));
                    continue;
                }

                if (entry.TypeKind == InspectorTypeKind.Struct)
                {
                    FillStructProperties(entry, itemTarget, section.Properties, in sw);
                    continue;
                }

                if (entry.TypeKind != InspectorTypeKind.Primitive &&
                    entry.TypeKind != InspectorTypeKind.PrimitiveStruct && entry.TypeKind != InspectorTypeKind.String)
                    continue;

                var value = FormatValue(itemTarget, new FormatOptions(entry.Format, entry.TypeKind), sw);
                section.Properties.Add(new UiTextProperty(new String16Utf8(entry.Name), new String16Utf8(value)));
            }
        }

        return array;
    }


    private static void FillStructProperties(InspectorFieldMeta meta, object target, List<UiTextProperty> properties,
        in UnsafeSpanWriter sw)
    {
        InspectorRegistry.TryGet(meta.Type, out var entries);
        foreach (var entry in entries)
        {
            var itemValue = entry.Getter(target);
            if (itemValue == null) continue;

            var value = FormatValue(itemValue, new FormatOptions(entry.Format, entry.TypeKind), sw);
            properties.Add(new UiTextProperty(new String16Utf8(entry.Name), new String16Utf8(value)));
        }
    }

    private static String8Utf8 GetPrimitiveStruct(InspectorFieldMeta meta, object target, in UnsafeSpanWriter sw)
    {
        InspectorRegistry.TryGet(meta.Type, out var entries);
        foreach (var it in entries)
        {
            if (it.TypeKind != InspectorTypeKind.Primitive) continue;
            var value = FormatValue(it.Getter(target), new FormatOptions(it.Format, it.TypeKind), sw);
            return new String8Utf8(value);
        }

        throw new ArgumentException($"No primitive id found for {meta.Type}");
    }


    private static ReadOnlySpan<byte> FormatValue(object? target, in FormatOptions formatOptions, UnsafeSpanWriter sw)
    {
        if (target == null) return "null"u8;

        var format = formatOptions.Format;

        if (target is bool bol) return FormatBool(bol, format, sw);

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

        static ReadOnlySpan<byte> FormatBool(bool value, string? format, UnsafeSpanWriter sw)
        {
            if (!string.IsNullOrWhiteSpace(format) && format.Length >= 3 && !char.IsWhiteSpace(format[0]))
            {
                var span = format.AsSpan();
                var splitIndex = span.IndexOf('/');
                if (splitIndex >= 0)
                {
                    return sw.Start(value ? span.Slice(0, splitIndex) : span.Slice(splitIndex + 1)).EndSpan();
                }
            }

            return sw.Start(value ? "true" : "false").EndSpan();
        }
    }
}
/*
    private static void BuildRecursive(in BuilderContext ctx, InspectorItem item, InspectorFieldMeta meta, object? target, int depth)
    {
        if (target == null)
        {
            item.Fields.Add(Row.Make("(null)", "null", depth));
            return;
        }

        // Leaf types
        if (meta.TypeKind == InspectorTypeKind.Unknown)
        {
            item.Fields.Add(Row.Make(meta.Name, "Unknown", depth));
            return;
        }

        if (meta.TypeKind is InspectorTypeKind.Primitive or InspectorTypeKind.String)
        {
            item.Fields.Add(Row.Make(meta.Name, FormatValue(target, default,ctx.Sw), depth));
            return;
        }

        // Dictionary
        if (meta.TypeKind == InspectorTypeKind.Map)
        {
            BuildMap(in ctx, meta, target, depth);
            return;
        }

        // Collection (array, list)
        if (meta.TypeKind == InspectorTypeKind.Array)
        {
            BuildArray(in ctx, meta, target, depth);
            return;
        }

        if (meta.TypeKind == InspectorTypeKind.PrimitiveStruct)
        {
            if (!InspectorRegistry.TryGet(meta.Type, out var entries)) return;
            var halfCap = ctx.Sw.Capacity / 2;
            var itemSw = ctx.Sw.GetSlicedWriter(0, halfCap);
            var formatSw = ctx.Sw.GetSlicedWriter(halfCap, halfCap*2);
            itemSw.Start('[');
            foreach (var it in entries)
            {
                var value = it.Getter(target);
                var valueUtf8 = FormatValue(value, new FormatOptions(it.Format, it.TypeKind), formatSw);
                itemSw.Append(valueUtf8).Append(",");
            }

            item.Fields.Add(Row.Make(meta.Name, itemSw.Append(']').EndSpan(), depth));
            return;
        }

        item.Fields.Add(Row.Make(meta.Type.Name, ReadOnlySpan<byte>.Empty, depth));
        BuildPropertyNode(in ctx, meta, target, depth);
    }


    private static void BuildPropertyNode(in BuilderContext ctx, InspectorFieldMeta meta, object target, int depth)
    {
        if (!InspectorRegistry.TryGet(meta.Type, out var entries)) return;

        var item = ctx.Inspector.AddItem(meta.Name, depth);
        foreach (var it in entries)
        {
            var value = it.Getter(target);

            if (it.TypeKind is InspectorTypeKind.Struct or InspectorTypeKind.Class)
            {
                BuildPropertyNode(in ctx, it, value!, depth + 1);
            }
            else
            {
                var valueUtf8 = FormatValue(value, new FormatOptions(it.Format, it.TypeKind),ctx.Sw);
                item.Fields.Add(Row.Make(it.Name, valueUtf8, depth + 1));
            }
        }
    }

    private static void BuildMap(in BuilderContext ctx, InspectorFieldMeta meta, object target, int depth)
    {
        var dict = (IDictionary)target;
        var item = ctx.Inspector.AddItem(meta.Name, depth);
        //item.Entries.Add(Row.Make(meta.Name, ReadOnlySpan<byte>.Empty, depth));
        foreach (DictionaryEntry kv in dict)
        {
            var value = kv.Value;
            var keyLabel = kv.Key as string ?? kv.Key.ToString() ?? "null";
            item.Fields.Add(Row.Make(keyLabel, FormatValue(value, default,ctx.Sw), depth + 1));


           if (value == null || !InspectorRegistry.TryGet(value.GetType(), out var entries))

            foreach (var it in entries)
            {
                var entry = inspectorEntry.AddChild(it);
                BuildRecursive(entry,meta, it.Getter(value), depth + 1);
            }
        }
    }

    private static void BuildArray(in BuilderContext ctx, InspectorFieldMeta meta, object target, int depth)
    {
        var list = (IList)target;
        var item = ctx.Inspector.AddItem(meta.Name, depth);
        var index = 0;

//        rows.Add(Row.Make(meta.Name, ReadOnlySpan<byte>.Empty, depth));
        foreach (var it in list)
        {
            item.Fields.Add(Row.Make($"[{index}]", FormatValue(item, default,ctx.Sw), depth + 1));
            index++;


            !InspectorRegistry.TryGet(item.GetType(), out var entries)
            foreach (var it in entries)
            {
                var value = it.Getter(item);
                var entry = inspectorEntry.AddChild(it);
                BuildRecursive(entry, it, value, depth + 1);
            }

        }
    }


}
*/