using System.Collections;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Editor.Lib.Reflection;

public static class InspectorBuilder
{
    public static unsafe InspectorEditorObject Build(Type type, object target)
    {
        if (!InspectorRegistry.TryGet(type, out var fieldMeta))
            throw new ArgumentException($"Type '{type}' is not a valid inspector type.");

        byte* buffer = stackalloc byte[128];
        var sw = new UnsafeSpanWriter(buffer, 128);

        var inspector = new InspectorEditorObject(type.Name, type);
        foreach (var meta in fieldMeta.AllFields)
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

        return inspector;
    }

    private static void BuildHeader(InspectorEditorObject inspector, InspectorFieldMeta fieldMeta, object? fieldTarget,
        in UnsafeSpanWriter sw)
    {
        if (fieldTarget == null || fieldMeta.TypeKind == InspectorTypeKind.Unknown) return;

        if (fieldMeta.FieldKind == InspectorFieldKind.Name)
            inspector.Header.Name = new String16Utf8((string)fieldTarget);

        if (fieldMeta.FieldKind == InspectorFieldKind.Generation)
        {
            var value = FormatValue(fieldTarget, default, sw);
            inspector.Header.Gen = new String8Utf8(value);
            return;
        }

        if (fieldMeta.FieldKind == InspectorFieldKind.Id)
        {
            inspector.Header.Id = GetPrimitiveStruct(fieldMeta, fieldTarget, in sw);
        }
    }

    private static InspectorSectionUi? BuildProperties(InspectorFieldMeta fieldMeta, object fieldTarget,
        in UnsafeSpanWriter sw)
    {
        if (!InspectorRegistry.TryGet(fieldMeta.Type, out var metas)) return null;

        var properties = new InspectorSectionUi(fieldMeta.Name) { Title = new String32Utf8(fieldMeta.Name) };
        foreach (var it in metas.ValueFields)
        {
            var value = FormatValue(it.Getter(fieldTarget), new FormatOptions(it.Format, it.TypeKind), sw);
            properties.Properties.Add(new UiTextProperty(new String16Utf8(it.Name), new String16Utf8(value)));
        }

        return properties;
    }

    private static InspectorArrayUi BuildArray(InspectorFieldMeta fieldMeta, object fieldTarget, UnsafeSpanWriter sw)
    {
        var list = (IList)fieldTarget;
        var index = 0;
        var array = new InspectorArrayUi(fieldMeta.Name, fieldMeta.Name, list.Count);

        foreach (var item in list)
        {
            if (!InspectorRegistry.TryGet(item.GetType(), out var metas)) continue;

            var section = new InspectorSectionUi(item.GetType().Name);
            array.Fields.Add(section);

            foreach (var entry in metas.AllFields)
            {
                var itemTarget = entry.Getter(item);
                if (itemTarget == null) continue;


                if (entry.FieldKind == InspectorFieldKind.Name)
                {
                    var strValue = (string)itemTarget;
                    section.Title =
                        new String32Utf8(sw.Append("[").Append(index).Append("] - ").Append(strValue).EndSpan());
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

                if (!entry.TypeKind.IsValue()) continue;

                var value = FormatValue(itemTarget, new FormatOptions(entry.Format, entry.TypeKind), sw);
                section.Properties.Add(new UiTextProperty(new String16Utf8(entry.Name), new String16Utf8(value)));
            }
        }

        return array;
    }


    private static void FillStructProperties(InspectorFieldMeta fieldMeta, object fieldTarget,
        List<UiTextProperty> properties,
        in UnsafeSpanWriter sw)
    {
        InspectorRegistry.TryGet(fieldMeta.Type, out var metas);
        foreach (var entry in metas.ValueFields)
        {
            var itemValue = entry.Getter(fieldTarget);
            if (itemValue == null) continue;

            var value = FormatValue(itemValue, new FormatOptions(entry.Format, entry.TypeKind), sw);
            properties.Add(new UiTextProperty(new String16Utf8(entry.Name), new String16Utf8(value)));
        }
    }

    private static String8Utf8 GetPrimitiveStruct(InspectorFieldMeta fieldMeta, object fieldTarget,
        in UnsafeSpanWriter sw)
    {
        InspectorRegistry.TryGet(fieldMeta.Type, out var metas);
        foreach (var it in metas.ValueFields)
        {
            var value = FormatValue(it.Getter(fieldTarget), new FormatOptions(it.Format, it.TypeKind), sw);
            return new String8Utf8(value);
        }

        throw new ArgumentException($"No primitive id found for {fieldMeta.Type}");
    }


    private static ReadOnlySpan<byte> FormatValue(object? target, in FormatOptions formatOptions, UnsafeSpanWriter sw)
    {
        if (target == null) return "null"u8;

        var format = formatOptions.Format;

        if (target is bool bol) return FormatBool(bol, format, sw);

        switch (target)
        {
            case byte b: sw.Append((int)b); break;
            case short s: sw.Append((int)s); break;
            case ushort us: sw.Append((int)us); break;
            case int i: sw.Append(i); break;
            case uint ui: sw.Append(ui); break;
            case float f: sw.Append(f, format); break;
            case double d: sw.Append(d, format); break;
            case long l: sw.Append(l, format); break;
            case ulong ul: sw.Append(ul, format); break;
            case Guid g: sw.Append(g, format); break;
            case DateTime dt: sw.Append(dt, format); break;
            case string s: sw.Append(s); break;
            default: sw.Append(target.ToString() ?? "null"); break;
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
                    return sw.Append(value ? span.Slice(0, splitIndex) : span.Slice(splitIndex + 1)).EndSpan();
                }
            }

            return sw.Append(value ? "true" : "false").EndSpan();
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
            itemsw.Append('[');
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